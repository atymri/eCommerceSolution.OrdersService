using eCommerce.BLL.DTOs;
using eCommerce.BLL.ServiceContracts;
using eCommerce.DAL.Entities;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;

namespace eCommerce.API.ApiControllers;

[Route("api/[controller]")]
[ApiController]
public class OrdersController : ControllerBase
{
    private readonly IOrdersService _service;
    private readonly IValidator<OrderAddRequest> _addRequestValidator;
    private readonly IValidator<OrderUpdateRequest> _updateRequestValidator;
    public OrdersController(IOrdersService service, 
        IValidator<OrderUpdateRequest> updateRequestValidator,
        IValidator<OrderAddRequest> addRequestValidator)
    {
        _service = service;
        _updateRequestValidator = updateRequestValidator;
        _addRequestValidator = addRequestValidator;
    }

    // api/orders
    [HttpGet]
    public async Task<IEnumerable<OrderResponse>> GetOrders()
        => await _service.GetOrdersAsync();

    [HttpGet("search/byorder/{id:guid}")]
    public async Task<ActionResult<OrderResponse>> GetOrderById(Guid id)
        => await _service
            .GetSingleOrderByConditionAsync(Builders<Order>.Filter.Eq(o => o.OrderID, id));

    [HttpGet("search/byproduct/{productId:guid}")]
    public async Task<IEnumerable<OrderResponse>> GetOrdersByProductId(Guid productId)
    {
        var filter = Builders<Order>.Filter.ElemMatch(o => o.OrderItems,
            Builders<OrderItem>.Filter.Eq(oi => oi.ProductID, productId));

        return await _service.GetMultipleOrdersByConditionAsync(filter);
    }

    [HttpGet("search/byuser/{userId:guid}")]
    public async Task<IEnumerable<OrderResponse>> GetOrdersByUserId(Guid userId)
    {
        var filter = Builders<Order>.Filter.Eq(o => o.UserID, userId);

        return await _service.GetMultipleOrdersByConditionAsync(filter);
    }

    [HttpGet("search/date/{date:datetime}")]
    public async Task<IEnumerable<OrderResponse>> GetOrdersInDate(DateTime date)
    {
        var start = date.Date;
        var end = date.Date.AddDays(1);

        var filter = Builders<Order>.Filter.And(
            Builders<Order>.Filter.Gte(o => o.OrderDate, start), 
            Builders<Order>.Filter.Lte(o => o.OrderDate, end));

        return await _service.GetMultipleOrdersByConditionAsync(filter);
    }

    [HttpPost]
    public async Task<ActionResult<OrderResponse>> PostOrder([FromBody]OrderAddRequest req)
    {
        if (req is null)
            return BadRequest();

        var res = await _service.AddOrderAsync(req);

        if (res is null)
            return Problem("Error in submitting order");

        return Created($"api/orders/search/{res.OrderId}",res);
    }

    [HttpPut("/{orderId:guid}")]
    public async Task<ActionResult<OrderResponse>> PutOrder(Guid orderId, [FromBody] OrderUpdateRequest req)
    {
        if (req is null || orderId == Guid.Empty)
            return BadRequest();

        if (orderId != req.OrderId)
            return BadRequest();

        var res = await _service.UpdateOrderAsync(req);
        if (res is null)
            return Problem("Error in updating order");

        return Ok();
    }

    [HttpDelete("/{orderId:guid}")]
    public async Task<ActionResult<bool>> DeleteOrder(Guid orderId)
    {
        if (orderId == Guid.Empty)
            return false;

        var filter = Builders<Order>.Filter.Eq(o => o.OrderID, orderId);
        if (await _service.GetSingleOrderByConditionAsync(filter) is null)
            return false;

        var res = await _service.DeleteOrderAsync(orderId);

        if (!res)
            return Problem("Error deleting order");

        return Ok();
    }
}
