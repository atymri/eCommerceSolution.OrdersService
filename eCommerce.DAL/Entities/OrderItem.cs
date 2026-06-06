using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace eCommerce.DAL.Entities;

public class OrderItem
{
    [BsonId]
    [BsonRepresentation(BsonType.String)]
    public Guid _id { get; set; } = Guid.NewGuid();

    [BsonRepresentation(BsonType.String)]
    public Guid ProductID { get; set; }

    [BsonRepresentation(BsonType.Decimal128)]
    public decimal UnitPrice { get; set; }

    [BsonRepresentation(BsonType.Int32)]
    public int Quantity { get; set; }

    [BsonRepresentation(BsonType.Decimal128)]
    public decimal TotalPrice { get; private set; }

    public void SetTotalPrice()
    {
        TotalPrice = Quantity != 0 ? UnitPrice * Quantity : 0;
    }
}
