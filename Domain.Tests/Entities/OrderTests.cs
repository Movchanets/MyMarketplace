using Domain.Entities;
using Domain.Enums;
using Domain.ValueObjects;
using FluentAssertions;
using Xunit;

namespace Domain.Tests.Entities;

public class OrderTests
{
	private static ShippingAddress CreateShippingAddress()
	{
		return new ShippingAddress(
			firstName: "John",
			lastName: "Doe",
			phoneNumber: "1234567890",
			email: "john@example.com",
			addressLine1: "123 Main St",
			addressLine2: null,
			city: "New York",
			state: "NY",
			postalCode: "10001",
			country: "USA"
		);
	}

	[Fact]
	public void Constructor_WithValidData_CreatesOrder()
	{
		// Arrange
		var userId = Guid.NewGuid();
		var shippingAddress = CreateShippingAddress();
		var deliveryMethod = "Standard Shipping";
		var paymentMethod = "Credit Card";

		// Act
		var order = new Order(userId, shippingAddress, deliveryMethod, paymentMethod);

		// Assert
		order.UserId.Should().Be(userId);
		order.Status.Should().Be(OrderStatus.Pending);
		order.PaymentStatus.Should().Be(PaymentStatus.Pending);
		order.ShippingAddress.Should().Be(shippingAddress);
		order.DeliveryMethod.Should().Be(deliveryMethod);
		order.PaymentMethod.Should().Be(paymentMethod);
		order.Items.Should().BeEmpty();
		order.OrderNumber.Should().NotBeNullOrEmpty();
		order.OrderNumber.Should().StartWith("ORD-");
	}

	[Fact]
	public void Constructor_WithEmptyUserId_ThrowsArgumentException()
	{
		// Arrange
		var shippingAddress = CreateShippingAddress();

		// Act
		Action act = () => new Order(Guid.Empty, shippingAddress, "Standard", "Card");

		// Assert
		act.Should().Throw<ArgumentException>().WithMessage("*UserId cannot be empty*");
	}

	[Fact]
	public void Constructor_WithNullShippingAddress_ThrowsArgumentNullException()
	{
		// Arrange
		var userId = Guid.NewGuid();

		// Act
		Action act = () => new Order(userId, null!, "Standard", "Card");

		// Assert
		act.Should().Throw<ArgumentNullException>();
	}

	[Fact]
	public void Constructor_WithEmptyDeliveryMethod_ThrowsArgumentException()
	{
		// Arrange
		var userId = Guid.NewGuid();
		var shippingAddress = CreateShippingAddress();

		// Act
		Action act = () => new Order(userId, shippingAddress, "", "Card");

		// Assert
		act.Should().Throw<ArgumentException>().WithMessage("*Delivery method is required*");
	}

	[Fact]
	public void Constructor_WithEmptyPaymentMethod_ThrowsArgumentException()
	{
		// Arrange
		var userId = Guid.NewGuid();
		var shippingAddress = CreateShippingAddress();

		// Act
		Action act = () => new Order(userId, shippingAddress, "Standard", "");

		// Assert
		act.Should().Throw<ArgumentException>().WithMessage("*Payment method is required*");
	}

	[Fact]
	public void AddItem_WithValidItem_AddsItemToOrder()
	{
		// Arrange
		var order = new Order(Guid.NewGuid(), CreateShippingAddress(), "Standard", "Card");
		var orderItem = new OrderItem(
			order.Id,
			Guid.NewGuid(),
			Guid.NewGuid(),
			quantity: 2,
			priceAtPurchase: 10.00m,
			productNameSnapshot: "Test Product",
			productImageUrlSnapshot: null,
			skuCodeSnapshot: "TEST-001",
			skuAttributesSnapshot: null
		);

		// Act
		order.AddItem(orderItem);

		// Assert
		order.Items.Should().HaveCount(1);
		order.GetTotalItems().Should().Be(2);
	}

	[Fact]
	public void AddItem_WhenOrderNotPending_ThrowsInvalidOperationException()
	{
		// Arrange
		var order = new Order(Guid.NewGuid(), CreateShippingAddress(), "Standard", "Card");
		order.UpdateStatus(OrderStatus.Confirmed);
		var orderItem = new OrderItem(
			order.Id,
			Guid.NewGuid(),
			Guid.NewGuid(),
			quantity: 1,
			priceAtPurchase: 10.00m,
			productNameSnapshot: "Test Product",
			productImageUrlSnapshot: null,
			skuCodeSnapshot: "TEST-001",
			skuAttributesSnapshot: null
		);

		// Act
		Action act = () => order.AddItem(orderItem);

		// Assert
		act.Should().Throw<InvalidOperationException>().WithMessage("*not pending*");
	}

	[Fact]
	public void UpdateStatus_WithValidTransition_UpdatesStatus()
	{
		// Arrange
		var order = new Order(Guid.NewGuid(), CreateShippingAddress(), "Standard", "Card");

		// Act
		order.UpdateStatus(OrderStatus.Confirmed);

		// Assert
		order.Status.Should().Be(OrderStatus.Confirmed);
	}

	[Fact]
	public void UpdateStatus_ToShipped_SetsShippedAt()
	{
		// Arrange
		var order = new Order(Guid.NewGuid(), CreateShippingAddress(), "Standard", "Card");
		order.UpdateStatus(OrderStatus.Confirmed);
		order.UpdateStatus(OrderStatus.Processing);

		// Act
		order.UpdateStatus(OrderStatus.Shipped);

		// Assert
		order.Status.Should().Be(OrderStatus.Shipped);
		order.ShippedAt.Should().NotBeNull();
	}

	[Fact]
	public void UpdateStatus_ToDelivered_SetsDeliveredAt()
	{
		// Arrange
		var order = new Order(Guid.NewGuid(), CreateShippingAddress(), "Standard", "Card");
		order.UpdateStatus(OrderStatus.Confirmed);
		order.UpdateStatus(OrderStatus.Processing);
		order.UpdateStatus(OrderStatus.Shipped);

		// Act
		order.UpdateStatus(OrderStatus.Delivered);

		// Assert
		order.Status.Should().Be(OrderStatus.Delivered);
		order.DeliveredAt.Should().NotBeNull();
	}

	[Fact]
	public void UpdateStatus_WithInvalidTransition_ThrowsInvalidOperationException()
	{
		// Arrange
		var order = new Order(Guid.NewGuid(), CreateShippingAddress(), "Standard", "Card");

		// Act
		Action act = () => order.UpdateStatus(OrderStatus.Delivered);

		// Assert
		act.Should().Throw<InvalidOperationException>().WithMessage("*Cannot transition*");
	}

	[Fact]
	public void Cancel_WithPendingStatus_CancelsOrder()
	{
		// Arrange
		var order = new Order(Guid.NewGuid(), CreateShippingAddress(), "Standard", "Card");
		var reason = "Changed my mind";

		// Act
		order.Cancel(reason);

		// Assert
		order.Status.Should().Be(OrderStatus.Cancelled);
		order.CancelledAt.Should().NotBeNull();
		order.CancellationReason.Should().Be(reason);
	}

	[Fact]
	public void Cancel_WithConfirmedStatus_CancelsOrder()
	{
		// Arrange
		var order = new Order(Guid.NewGuid(), CreateShippingAddress(), "Standard", "Card");
		order.UpdateStatus(OrderStatus.Confirmed);

		// Act
		order.Cancel();

		// Assert
		order.Status.Should().Be(OrderStatus.Cancelled);
	}

	[Fact]
	public void Cancel_WithShippedStatus_ThrowsInvalidOperationException()
	{
		// Arrange
		var order = new Order(Guid.NewGuid(), CreateShippingAddress(), "Standard", "Card");
		order.UpdateStatus(OrderStatus.Confirmed);
		order.UpdateStatus(OrderStatus.Processing);
		order.UpdateStatus(OrderStatus.Shipped);

		// Act
		Action act = () => order.Cancel();

		// Assert
		act.Should().Throw<InvalidOperationException>().WithMessage("*Cannot cancel*");
	}

	[Fact]
	public void SetTrackingInfo_WithValidData_SetsTracking()
	{
		// Arrange
		var order = new Order(Guid.NewGuid(), CreateShippingAddress(), "Standard", "Card");
		var trackingNumber = "TRACK123456";
		var carrier = "UPS";

		// Act
		order.SetTrackingInfo(trackingNumber, carrier);

		// Assert
		order.TrackingNumber.Should().Be(trackingNumber);
		order.ShippingCarrier.Should().Be(carrier);
	}

	[Fact]
	public void SetTrackingInfo_WithEmptyTrackingNumber_ThrowsArgumentException()
	{
		// Arrange
		var order = new Order(Guid.NewGuid(), CreateShippingAddress(), "Standard", "Card");

		// Act
		Action act = () => order.SetTrackingInfo("", "UPS");

		// Assert
		act.Should().Throw<ArgumentException>().WithMessage("*Tracking number is required*");
	}

	[Fact]
	public void SetTrackingInfo_WithEmptyCarrier_ThrowsArgumentException()
	{
		// Arrange
		var order = new Order(Guid.NewGuid(), CreateShippingAddress(), "Standard", "Card");

		// Act
		Action act = () => order.SetTrackingInfo("TRACK123", "");

		// Assert
		act.Should().Throw<ArgumentException>().WithMessage("*Carrier is required*");
	}

	[Fact]
	public void ApplyPromoCode_WithValidCode_AppliesDiscount()
	{
		// Arrange
		var order = new Order(Guid.NewGuid(), CreateShippingAddress(), "Standard", "Card");
		var promoCode = "SAVE10";
		var discount = 10.00m;

		// Act
		order.ApplyPromoCode(promoCode, discount);

		// Assert
		order.PromoCode.Should().Be(promoCode);
		order.DiscountAmount.Should().Be(discount);
	}

	[Fact]
	public void ApplyPromoCode_WhenOrderNotPending_ThrowsInvalidOperationException()
	{
		// Arrange
		var order = new Order(Guid.NewGuid(), CreateShippingAddress(), "Standard", "Card");
		order.UpdateStatus(OrderStatus.Confirmed);

		// Act
		Action act = () => order.ApplyPromoCode("SAVE10", 10.00m);

		// Assert
		act.Should().Throw<InvalidOperationException>().WithMessage("*not pending*");
	}

	[Fact]
	public void SetShippingCost_WithValidCost_SetsCost()
	{
		// Arrange
		var order = new Order(Guid.NewGuid(), CreateShippingAddress(), "Standard", "Card");
		var shippingCost = 15.00m;

		// Act
		order.SetShippingCost(shippingCost);

		// Assert
		order.ShippingCost.Should().Be(shippingCost);
	}

	[Fact]
	public void SetShippingCost_WithNegativeCost_ThrowsArgumentException()
	{
		// Arrange
		var order = new Order(Guid.NewGuid(), CreateShippingAddress(), "Standard", "Card");

		// Act
		Action act = () => order.SetShippingCost(-5.00m);

		// Assert
		act.Should().Throw<ArgumentException>().WithMessage("*cannot be negative*");
	}

	[Fact]
	public void SetCustomerNotes_WithValidNotes_SetsNotes()
	{
		// Arrange
		var order = new Order(Guid.NewGuid(), CreateShippingAddress(), "Standard", "Card");
		var notes = "Please leave at the door";

		// Act
		order.SetCustomerNotes(notes);

		// Assert
		order.CustomerNotes.Should().Be(notes);
	}

	[Fact]
	public void UpdatePaymentStatus_UpdatesStatus()
	{
		// Arrange
		var order = new Order(Guid.NewGuid(), CreateShippingAddress(), "Standard", "Card");

		// Act
		order.UpdatePaymentStatus(PaymentStatus.Paid);

		// Assert
		order.PaymentStatus.Should().Be(PaymentStatus.Paid);
	}

	[Fact]
	public void GetTotalItems_WithMultipleItems_ReturnsSum()
	{
		// Arrange
		var order = new Order(Guid.NewGuid(), CreateShippingAddress(), "Standard", "Card");
		order.AddItem(new OrderItem(order.Id, Guid.NewGuid(), Guid.NewGuid(), 2, 10.00m, "Product 1", null, "SKU1", null));
		order.AddItem(new OrderItem(order.Id, Guid.NewGuid(), Guid.NewGuid(), 3, 15.00m, "Product 2", null, "SKU2", null));

		// Act & Assert
		order.GetTotalItems().Should().Be(5);
	}
}
