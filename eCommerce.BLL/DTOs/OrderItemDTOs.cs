namespace eCommerce.BLL.DTOs;

public sealed record OrderItemResponse(
        Guid ProductId,
        string? ProductName,
        string? Category,
        int Quantity,
        decimal TotalPrice
    )
{
    public OrderItemResponse() : this(default, default, default, default, default)
    { }
};

public sealed record OrderItemAddRequest(
        Guid ProductId,
        int Quantity
    ) 
{
    public OrderItemAddRequest() : this(default, default)
    { }
}

public sealed record OrderItemUpdateRequest(
        Guid ProductId,
        int Quantity
    ) 
{
    public OrderItemUpdateRequest() : this(default, default)
    { }
}