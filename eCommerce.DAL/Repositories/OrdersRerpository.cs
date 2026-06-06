using eCommerce.DAL.Entities;
using eCommerce.DAL.RepositoryContracts;
using MongoDB.Driver;

namespace eCommerce.DAL.Repositories;

internal class OrdersRerpository : IOrdersRepository
{
    private readonly IMongoCollection<Order> _orders;
    private const string COLLECTION_NAME = "orders";

    public OrdersRerpository(IMongoDatabase database)
    {
        _orders = database.GetCollection<Order>(COLLECTION_NAME);
    }

    public async Task<Order?> AddOrderAsync(Order order)
    {
        order.OrderID = order._id;

        foreach(var item in order.OrderItems)
            item._id = Guid.NewGuid();

        await _orders.InsertOneAsync(order);
        return order;
    }

    public async Task<Order?> UpdateOrderAsync(Order order)
    {
        var filter = Builders<Order>.Filter.Eq(o => o.OrderID, order.OrderID);

        var options = new FindOneAndReplaceOptions<Order>()
        {
            ReturnDocument = ReturnDocument.After
        };

        var updated = await _orders.FindOneAndReplaceAsync(filter, order, options);
        return updated;
    }

    public async Task<bool> DeleteOrderAsync(Guid orderId)
        => (await _orders
                .DeleteOneAsync(Builders<Order>.Filter.Eq(o => o.OrderID, orderId))
            ).DeletedCount > 0;

    public async Task<IEnumerable<Order>> GetMultipleOrdersByConditionAsync(FilterDefinition<Order> filter)
        => await _orders.Find(filter).ToListAsync();

    public async Task<Order> GetSingleOrderByConditionAsync(FilterDefinition<Order> filter)
        => await _orders.Find(filter).FirstOrDefaultAsync();

    public async Task<IEnumerable<Order>> GetOrdersAsync()
        => await _orders.Find(Builders<Order>.Filter.Empty).ToListAsync();

}
