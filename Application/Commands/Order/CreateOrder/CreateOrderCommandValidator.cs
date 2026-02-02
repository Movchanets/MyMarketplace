using FluentValidation;

namespace Application.Commands.Order.CreateOrder;

/// <summary>
/// FluentValidation validator for CreateOrderCommand
/// </summary>
public class CreateOrderCommandValidator : AbstractValidator<CreateOrderCommand>
{
	public CreateOrderCommandValidator()
	{
		RuleFor(x => x.UserId)
			.NotEmpty().WithMessage("User ID is required");

		RuleFor(x => x.ShippingAddress)
			.NotNull().WithMessage("Shipping address is required");

		RuleFor(x => x.ShippingAddress.FirstName)
			.NotEmpty().WithMessage("First name is required")
			.MaximumLength(100).WithMessage("First name cannot exceed 100 characters");

		RuleFor(x => x.ShippingAddress.LastName)
			.NotEmpty().WithMessage("Last name is required")
			.MaximumLength(100).WithMessage("Last name cannot exceed 100 characters");

		RuleFor(x => x.ShippingAddress.PhoneNumber)
			.NotEmpty().WithMessage("Phone number is required")
			.MaximumLength(20).WithMessage("Phone number cannot exceed 20 characters");

		RuleFor(x => x.ShippingAddress.Email)
			.NotEmpty().WithMessage("Email is required")
			.EmailAddress().WithMessage("Valid email is required")
			.MaximumLength(100).WithMessage("Email cannot exceed 100 characters");

		RuleFor(x => x.ShippingAddress.AddressLine1)
			.NotEmpty().WithMessage("Address line 1 is required")
			.MaximumLength(200).WithMessage("Address line 1 cannot exceed 200 characters");

		RuleFor(x => x.ShippingAddress.AddressLine2)
			.MaximumLength(200).WithMessage("Address line 2 cannot exceed 200 characters")
			.When(x => !string.IsNullOrEmpty(x.ShippingAddress.AddressLine2));

		RuleFor(x => x.ShippingAddress.City)
			.NotEmpty().WithMessage("City is required")
			.MaximumLength(100).WithMessage("City cannot exceed 100 characters");

		RuleFor(x => x.ShippingAddress.State)
			.MaximumLength(100).WithMessage("State cannot exceed 100 characters")
			.When(x => !string.IsNullOrEmpty(x.ShippingAddress.State));

		RuleFor(x => x.ShippingAddress.PostalCode)
			.NotEmpty().WithMessage("Postal code is required")
			.MaximumLength(20).WithMessage("Postal code cannot exceed 20 characters");

		RuleFor(x => x.ShippingAddress.Country)
			.NotEmpty().WithMessage("Country is required")
			.MaximumLength(100).WithMessage("Country cannot exceed 100 characters");

		RuleFor(x => x.DeliveryMethod)
			.NotEmpty().WithMessage("Delivery method is required")
			.MaximumLength(50).WithMessage("Delivery method cannot exceed 50 characters");

		RuleFor(x => x.PaymentMethod)
			.NotEmpty().WithMessage("Payment method is required")
			.MaximumLength(50).WithMessage("Payment method cannot exceed 50 characters");

		RuleFor(x => x.CustomerNotes)
			.MaximumLength(1000).WithMessage("Customer notes cannot exceed 1000 characters")
			.When(x => !string.IsNullOrEmpty(x.CustomerNotes));

		RuleFor(x => x.PromoCode)
			.MaximumLength(50).WithMessage("Promo code cannot exceed 50 characters")
			.When(x => !string.IsNullOrEmpty(x.PromoCode));

		RuleFor(x => x.IdempotencyKey)
			.MaximumLength(100).WithMessage("Idempotency key cannot exceed 100 characters")
			.When(x => !string.IsNullOrEmpty(x.IdempotencyKey));
	}
}
