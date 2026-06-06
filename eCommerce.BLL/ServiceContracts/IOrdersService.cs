using eCommerce.BLL.DTOs;
using eCommerce.DAL.Entities;
using MongoDB.Driver;

namespace eCommerce.BLL.ServiceContracts;

public interface IOrdersService
{
    Task<List<OrderResponse>> GetOrdersAsync();
    Task<List<OrderResponse>> GetMultipleOrdersByConditionAsync(FilterDefinition<Order> filter);
    Task<OrderResponse> GetSingleOrderByConditionAsync(FilterDefinition<Order> filter);
    Task<OrderResponse?> AddOrderAsync(OrderAddRequest request);
    Task<OrderResponse?> UpdateOrderAsync(OrderUpdateRequest request);
    Task<bool> DeleteOrderAsync(Guid orderId);
}
