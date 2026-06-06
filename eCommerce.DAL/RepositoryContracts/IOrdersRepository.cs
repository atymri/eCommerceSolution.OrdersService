using eCommerce.DAL.Entities;
using MongoDB.Driver;

namespace eCommerce.DAL.RepositoryContracts;

public interface IOrdersRepository
{
    Task<IEnumerable<Order>> GetOrdersAsync();
    Task<IEnumerable<Order>> GetMultipleOrdersByConditionAsync(FilterDefinition<Order> filter);
    Task<Order> GetSingleOrderByConditionAsync(FilterDefinition<Order> filter);
    Task<Order?> AddOrderAsync(Order order);
    Task<Order?> UpdateOrderAsync(Order order);
    Task<bool> DeleteOrderAsync(Guid orderId);
}
