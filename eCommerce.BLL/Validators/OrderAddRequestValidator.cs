using eCommerce.BLL.DTOs;
using FluentValidation;

namespace eCommerce.BLL.Validators;

public class OrderAddRequestValidator : AbstractValidator<OrderAddRequest>
{
    public OrderAddRequestValidator()
    {
        RuleFor(req => req.UserId)
            .NotEmpty().WithMessage("user id can not be empty")
            .Must(userId => userId != Guid.Empty).WithMessage("Invalid Userid");
        
        RuleFor(req => req.OrderDate)
            .NotEmpty().WithMessage("order date can not be empty")
            .LessThanOrEqualTo(DateTime.UtcNow).WithMessage("order date cannot be in the future");

        RuleFor(req => req.Items)
            .NotEmpty().WithMessage("order must contain at least one item")
            .Must(item => item != null && item.Count > 0).WithMessage("order must contain at least 1 item");
    }
}
