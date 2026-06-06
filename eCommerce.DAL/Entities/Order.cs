using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace eCommerce.DAL.Entities;

public class Order
{
    [BsonId]
    [BsonRepresentation(BsonType.String)]
    public Guid _id { get; set; } = Guid.NewGuid();

    [BsonRepresentation(BsonType.String)]
    public Guid OrderID { get; set; }

    [BsonRepresentation(BsonType.String)]
    public Guid UserID { get; set; }

    [BsonRepresentation(BsonType.DateTime)]
    public DateTime OrderDate { get; set; } = DateTime.UtcNow;

    [BsonRepresentation(BsonType.Decimal128)]
    public decimal TotalBill { get; private set; }

    public List<OrderItem> OrderItems { get; set; } = new();

    public void RecalculateTotalBill()
    {       
        foreach (var item in OrderItems)
            item.SetTotalPrice();
        TotalBill = OrderItems.Sum(i => i.TotalPrice);
    }
}