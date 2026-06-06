using AutoMapper;
using eCommerce.BLL.DTOs;
using eCommerce.BLL.HttpClients;
using eCommerce.BLL.ServiceContracts;
using eCommerce.BusinessLogicLayer.DTOs;
using eCommerce.DAL.Entities;
using eCommerce.DAL.RepositoryContracts;
using FluentValidation;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Caching.Distributed;
using MongoDB.Driver;
using System.Net.WebSockets;
using System.Text;

namespace eCommerce.BLL.Services;

public class OrdersService : IOrdersService
{
    private readonly IOrdersRepository _repository;
    private readonly IMapper _mapper;
    private readonly IValidator<OrderAddRequest> _addValidator;
    private readonly IValidator<OrderUpdateRequest> _updateValidator;
    private readonly IValidator<OrderItemAddRequest> _itemAddRequestValidator;
    private readonly IValidator<OrderItemUpdateRequest> _itemUpdateRequestValidator;
    private readonly UsersHttpClient _userHttpClient;
    private readonly ProductsHttpClient _productsHttpClient;

    public OrdersService(
        IOrdersRepository repository,
        IMapper mapper,
        IValidator<OrderAddRequest> addValidator, // fluent validation for all.
        IValidator<OrderUpdateRequest> updateValidator,
        IValidator<OrderItemAddRequest> itemAddRequest,
        IValidator<OrderItemUpdateRequest> itemUpdateRequest,
        UsersHttpClient userHttpClient,
        ProductsHttpClient productsHttpClient)
    {
        _repository = repository;
        _mapper = mapper;
        _addValidator = addValidator;
        _updateValidator = updateValidator;
        _itemAddRequestValidator = itemAddRequest;
        _itemUpdateRequestValidator = itemUpdateRequest;
        _userHttpClient = userHttpClient;
        _productsHttpClient = productsHttpClient;
    }

    #region CUD Methods

    public async Task<OrderResponse?> AddOrderAsync(OrderAddRequest request)
    {
        ArgumentNullException.ThrowIfNull(request, nameof(request));

        var validationRes = await _addValidator.ValidateAsync(request);
        if (!validationRes.IsValid)
            throw new ArgumentException(string.Join(", ", validationRes.Errors.Select(e => e.ErrorMessage)));

        await ValidateItemsAsync(request.Items, _itemAddRequestValidator);
        await ValidateUserAsync(request.UserId);

        var productDetails = await ValidateProductsAsync(request.Items
            .Select(i => i.ProductId)
            .Distinct());

        // TODO: add a logic to decrease the product quantity in case of placing orders HERE
        // you have to add an endpoint in products service to decrease the quantity of items in the order
        
        var order = _mapper.Map<Order>(request);
        foreach (var item in order.OrderItems)
            item.UnitPrice = productDetails.Products
                .First(p => p.ProductID == item.ProductID).UnitPrice;
        order.RecalculateTotalBill();

        var insertedOrder = await _repository.AddOrderAsync(order);
        var enriched = await EnrichOrdersWithUsersAndProductsAsync(new[] { insertedOrder });

        return enriched?.FirstOrDefault();
    }

    public async Task<OrderResponse?> UpdateOrderAsync(OrderUpdateRequest request)
    {
        ArgumentNullException.ThrowIfNull(request, nameof(request));

        var validationRes = await _updateValidator.ValidateAsync(request);
        if (!validationRes.IsValid)
            throw new ArgumentException(string.Join(", ", validationRes.Errors.Select(e => e.ErrorMessage)));

        await ValidateItemsAsync(request.Items, _itemUpdateRequestValidator);
        await ValidateUserAsync(request.UserId);
        var productDetails = await ValidateProductsAsync(request.Items.Select(i => i.ProductId));

        var order = _mapper.Map<Order>(request);
        order._id = request.OrderId;

        foreach (var item in order.OrderItems)
            item.UnitPrice = productDetails.Products.First(p => p.ProductID == item.ProductID).UnitPrice;
        order.RecalculateTotalBill();

        var updatedOrder = await _repository.UpdateOrderAsync(order);

        var enriched = await EnrichOrdersWithUsersAndProductsAsync(new[] { updatedOrder });
        return enriched?.FirstOrDefault();
    }

    public async Task<bool> DeleteOrderAsync(Guid orderId)
    {
        var existing = await _repository.GetSingleOrderByConditionAsync(
            Builders<Order>.Filter.Eq(o => o.OrderID, orderId));

        return existing != null ?
            await _repository.DeleteOrderAsync(orderId)
            : false;
    }

    #endregion

    #region Get Methods

    public async Task<List<OrderResponse>> GetOrdersAsync()
    {
        var orders = await _repository.GetOrdersAsync();
        var result =  await EnrichOrdersWithUsersAndProductsAsync(orders);


        return result;
    }

    public async Task<List<OrderResponse>> GetMultipleOrdersByConditionAsync(FilterDefinition<Order> filter)
    {
        var orders = await _repository.GetMultipleOrdersByConditionAsync(filter);
        return await EnrichOrdersWithUsersAndProductsAsync(orders);
    }

    public async Task<OrderResponse> GetSingleOrderByConditionAsync(FilterDefinition<Order> filter)
    {
        var order = await _repository.GetSingleOrderByConditionAsync(filter);
        if (order == null) throw new KeyNotFoundException("Order not found");

        var enriched = await EnrichOrdersWithUsersAndProductsAsync(new[] { order });
        return enriched.First();
    }

    #endregion

    #region Helpers

    private async Task<List<OrderResponse>?> EnrichOrdersWithUsersAndProductsAsync(IEnumerable<Order> orders)
    {
        if (orders is null || !orders.Any())
            return null;

        var responses = _mapper.Map<List<OrderResponse>>(orders);
        
        var userIds = orders
            .Select(u => u.UserID)
            .Distinct()
            .ToList();
        
        var productIds = orders
            .SelectMany(o => o.OrderItems)
            .Select(oi => oi.ProductID)
            .Distinct()
            .ToList();

        var productTask = ValidateProductsAsync(productIds);
        var usersTask = ValidateUsersAsync(userIds);

        await Task.WhenAll(productTask, usersTask);

        var userById = usersTask.Result.ToDictionary(u => u.userId);
        var productById = productTask.Result.Products.ToDictionary(p => p.ProductID);


        foreach (var response in responses)
        {

            if (userById.TryGetValue(response.UserId, out var user))
                _mapper.Map(user, response);

            foreach (var item in response.Items)
            {
                if (productById.TryGetValue(item.ProductId, out var product))
                    _mapper.Map(product, item);
            }
        }

        return responses.OrderByDescending(r => r.OrderDate).ToList();
    }
        
    // rewrite this peace of shit asap, this makes the world a worse place.
    private async Task<ProductValidationResult> ValidateProductsAsync(IEnumerable<Guid> productIds)
    {
        var ids = productIds?.Distinct().ToList()
            ?? throw new ArgumentNullException(nameof(productIds));

        var tasks = ids.Select(id => _productsHttpClient.ValidateProductIDAsync(id));
        var results = await Task.WhenAll(tasks);

        var validProducts = new List<ProductSummery>();
        var invalidIds = new List<Guid>();

        for (int i = 0; i < ids.Count; i++)
        {
            if (results[i] is null)
                invalidIds.Add(ids[i]);
            else
                validProducts.Add(results[i]!);
        }

        return new ProductValidationResult(
            IsSuccess: invalidIds.Count == 0,
            Message: invalidIds.Any() ? "there are some invalid ids" : string.Empty,
            InvalidIDs: invalidIds,
            Products: validProducts
        );
    }


    private async Task<UserRersponse> ValidateUserAsync(Guid userId)
     => await _userHttpClient.GetUserByIDAsync(userId) is not UserRersponse user
        ? throw new ArgumentException(nameof(userId))
        : user;

    private async Task<List<UserRersponse>> ValidateUsersAsync(List<Guid> userIds)
    {
        var result = await _userHttpClient.ValidateUsers(userIds);
        if (!result.IsSuccess)
            throw new ArgumentException($"Invalid user ids: {string.Join(", ", result.InvalidGuids)}");

        return result.Users;
    }

    private async Task ValidateItemsAsync<TItem>(IEnumerable<TItem> items, IValidator<TItem> validator)
    {
        ArgumentNullException.ThrowIfNull(items, nameof(items));

        var validationTasks = items.Select(i => validator.ValidateAsync(i));
        var results = await Task.WhenAll(validationTasks);

        foreach (var result in results)
        {
            if (!result.IsValid)
                throw new ArgumentException(string.Join(", ", result.Errors.Select(e => e.ErrorMessage)));
        }
    }

    #endregion
}
