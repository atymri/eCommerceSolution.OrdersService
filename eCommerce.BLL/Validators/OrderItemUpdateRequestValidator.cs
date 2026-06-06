using eCommerce.BLL.DTOs;
using FluentValidation;

namespace eCommerce.BLL.Validators;

public class OrderItemUpdateRequestValidator : AbstractValidator<OrderItemUpdateRequest>
{
    public OrderItemUpdateRequestValidator()
    {
        RuleFor(req => req.ProductId)
            .NotEmpty().WithMessage("product id can not be empty")
            .Must(productId => productId != Guid.Empty).WithMessage("Invalid ProductID");

        //RuleFor(req => req.UnitPrice)
        //    .NotEmpty().WithMessage("unit price can not be empty")
        //    .GreaterThanOrEqualTo(0).WithMessage("unit price must be greater than zero");

        RuleFor(req => req.Quantity)
            .NotEmpty().WithMessage("quantity can not be empty")
            .GreaterThan(0).WithMessage("quantity must be greater than zero");
    }
}
