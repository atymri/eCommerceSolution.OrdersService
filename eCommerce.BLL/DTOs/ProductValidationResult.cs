namespace eCommerce.BusinessLogicLayer.DTOs;

public record ProductValidationResult(bool IsSuccess, string Message, List<Guid> InvalidIDs, List<ProductSummery> Products);
public record ProductSummery(Guid ProductID, string ProductName, string Category, decimal UnitPrice);