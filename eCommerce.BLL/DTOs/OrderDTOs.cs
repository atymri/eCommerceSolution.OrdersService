using System.Collections.ObjectModel;

namespace eCommerce.BLL.DTOs;

public class OrderResponse
{
    public Guid OrderId { get; set; }
    public Guid UserId { get; set; }
    public string PersonName { get; set; }
    public string Email { get; set; }
    public DateTime OrderDate { get; set; }
    public decimal TotalBill { get; set; }
    public List<OrderItemResponse> Items { get; set; } = new List<OrderItemResponse>();

};

public sealed record OrderAddRequest(
        Guid UserId,
        DateTime OrderDate,
        IReadOnlyCollection<OrderItemAddRequest> Items
    ) 
{
    public OrderAddRequest() : this(default, default, default)
    { }
};

public sealed record OrderUpdateRequest(
        Guid OrderId,
        Guid UserId,
        DateTime OrderDate,
        IReadOnlyCollection<OrderItemUpdateRequest> Items
    ) 
{
    public OrderUpdateRequest() : this(default, default, default, default)
    { }
};
