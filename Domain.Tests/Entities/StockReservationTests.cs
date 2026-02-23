using Domain.Entities;
using Domain.Enums;
using FluentAssertions;
using Xunit;

namespace Domain.Tests.Entities;

public class StockReservationTests
{
	#region Construction

	[Fact]
	public void Constructor_WithValidParams_CreatesActiveReservation()
	{
		// Arrange
		var skuId = Guid.NewGuid();
		var cartId = Guid.NewGuid();

		// Act
		var reservation = new StockReservation(skuId, quantity: 3, cartId: cartId);

		// Assert
		reservation.Id.Should().NotBe(Guid.Empty);
		reservation.SkuId.Should().Be(skuId);
		reservation.CartId.Should().Be(cartId);
		reservation.Quantity.Should().Be(3);
		reservation.Status.Should().Be(ReservationStatus.Active);
		reservation.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
		reservation.ExpiresAt.Should().BeCloseTo(
			DateTime.UtcNow.AddMinutes(StockReservation.DefaultTtlMinutes),
			TimeSpan.FromSeconds(2));
		reservation.OrderId.Should().BeNull();
		reservation.CancellationReason.Should().BeNull();
	}

	[Fact]
	public void Constructor_WithDefaultTtl_SetsExpiryTo15Minutes()
	{
		// Arrange & Act
		var reservation = new StockReservation(Guid.NewGuid(), quantity: 1);

		// Assert
		StockReservation.DefaultTtlMinutes.Should().Be(15);
		var expectedExpiry = reservation.CreatedAt.AddMinutes(15);
		reservation.ExpiresAt.Should().BeCloseTo(expectedExpiry, TimeSpan.FromMilliseconds(100));
	}

	[Fact]
	public void Constructor_WithCustomTtl_SetsCorrectExpiry()
	{
		// Arrange & Act
		var reservation = new StockReservation(Guid.NewGuid(), quantity: 1, ttlMinutes: 30);

		// Assert
		var expectedExpiry = reservation.CreatedAt.AddMinutes(30);
		reservation.ExpiresAt.Should().BeCloseTo(expectedExpiry, TimeSpan.FromMilliseconds(100));
	}

	[Fact]
	public void Constructor_WithSessionId_SetsSessionId()
	{
		// Arrange & Act
		var reservation = new StockReservation(Guid.NewGuid(), 1, sessionId: "guest-session-123");

		// Assert
		reservation.SessionId.Should().Be("guest-session-123");
		reservation.CartId.Should().BeNull();
	}

	[Fact]
	public void Constructor_WithEmptySkuId_ThrowsArgumentException()
	{
		// Act
		Action act = () => new StockReservation(Guid.Empty, 1);

		// Assert
		act.Should().Throw<ArgumentException>().WithParameterName("skuId");
	}

	[Fact]
	public void Constructor_WithZeroQuantity_ThrowsArgumentOutOfRangeException()
	{
		// Act
		Action act = () => new StockReservation(Guid.NewGuid(), 0);

		// Assert
		act.Should().Throw<ArgumentOutOfRangeException>().WithParameterName("quantity");
	}

	[Fact]
	public void Constructor_WithNegativeQuantity_ThrowsArgumentOutOfRangeException()
	{
		// Act
		Action act = () => new StockReservation(Guid.NewGuid(), -1);

		// Assert
		act.Should().Throw<ArgumentOutOfRangeException>().WithParameterName("quantity");
	}

	[Fact]
	public void Constructor_WithZeroTtl_ThrowsArgumentOutOfRangeException()
	{
		// Act
		Action act = () => new StockReservation(Guid.NewGuid(), 1, ttlMinutes: 0);

		// Assert
		act.Should().Throw<ArgumentOutOfRangeException>().WithParameterName("ttlMinutes");
	}

	#endregion

	#region IsExpired

	[Fact]
	public void IsExpired_WhenJustCreated_ReturnsFalse()
	{
		// Arrange
		var reservation = new StockReservation(Guid.NewGuid(), 1);

		// Act & Assert
		reservation.IsExpired().Should().BeFalse();
	}

	[Fact]
	public void IsExpired_WhenExpiresAtInPast_ReturnsTrue()
	{
		// Arrange
		var reservation = new StockReservation(Guid.NewGuid(), 1, ttlMinutes: 1);
		// Use reflection to backdate ExpiresAt
		typeof(StockReservation).GetProperty("ExpiresAt")!
			.SetValue(reservation, DateTime.UtcNow.AddMinutes(-5));

		// Act & Assert
		reservation.IsExpired().Should().BeTrue();
	}

	#endregion

	#region GetTimeRemaining

	[Fact]
	public void GetTimeRemaining_WhenActive_ReturnsPositiveTimeSpan()
	{
		// Arrange
		var reservation = new StockReservation(Guid.NewGuid(), 1, ttlMinutes: 15);

		// Act
		var remaining = reservation.GetTimeRemaining();

		// Assert
		remaining.Should().BeGreaterThan(TimeSpan.Zero);
		remaining.Should().BeLessThanOrEqualTo(TimeSpan.FromMinutes(15));
	}

	[Fact]
	public void GetTimeRemaining_WhenExpired_ReturnsZero()
	{
		// Arrange
		var reservation = new StockReservation(Guid.NewGuid(), 1);
		typeof(StockReservation).GetProperty("ExpiresAt")!
			.SetValue(reservation, DateTime.UtcNow.AddMinutes(-1));

		// Act
		var remaining = reservation.GetTimeRemaining();

		// Assert
		remaining.Should().Be(TimeSpan.Zero);
	}

	#endregion

	#region ConvertToOrder

	[Fact]
	public void ConvertToOrder_WhenActive_ChangesStatusToConverted()
	{
		// Arrange
		var reservation = new StockReservation(Guid.NewGuid(), 2, cartId: Guid.NewGuid());
		var orderId = Guid.NewGuid();

		// Act
		reservation.ConvertToOrder(orderId);

		// Assert
		reservation.Status.Should().Be(ReservationStatus.Converted);
		reservation.OrderId.Should().Be(orderId);
		reservation.UpdatedAt.Should().NotBeNull();
	}

	[Fact]
	public void ConvertToOrder_WhenAlreadyConverted_ThrowsInvalidOperationException()
	{
		// Arrange
		var reservation = new StockReservation(Guid.NewGuid(), 1);
		reservation.ConvertToOrder(Guid.NewGuid());

		// Act
		Action act = () => reservation.ConvertToOrder(Guid.NewGuid());

		// Assert
		act.Should().Throw<InvalidOperationException>();
	}

	[Fact]
	public void ConvertToOrder_WhenExpired_ThrowsInvalidOperationException()
	{
		// Arrange
		var reservation = new StockReservation(Guid.NewGuid(), 1);
		typeof(StockReservation).GetProperty("ExpiresAt")!
			.SetValue(reservation, DateTime.UtcNow.AddMinutes(-5));
		reservation.MarkAsExpired();

		// Act
		Action act = () => reservation.ConvertToOrder(Guid.NewGuid());

		// Assert
		act.Should().Throw<InvalidOperationException>();
	}

	[Fact]
	public void ConvertToOrder_WithEmptyOrderId_ThrowsArgumentException()
	{
		// Arrange
		var reservation = new StockReservation(Guid.NewGuid(), 1);

		// Act
		Action act = () => reservation.ConvertToOrder(Guid.Empty);

		// Assert
		act.Should().Throw<ArgumentException>().WithParameterName("orderId");
	}

	#endregion

	#region Cancel

	[Fact]
	public void Cancel_WhenActive_ChangesStatusToCancelled()
	{
		// Arrange
		var reservation = new StockReservation(Guid.NewGuid(), 1);

		// Act
		reservation.Cancel("User cancelled checkout");

		// Assert
		reservation.Status.Should().Be(ReservationStatus.Cancelled);
		reservation.CancellationReason.Should().Be("User cancelled checkout");
		reservation.UpdatedAt.Should().NotBeNull();
	}

	[Fact]
	public void Cancel_WhenAlreadyCancelled_ThrowsInvalidOperationException()
	{
		// Arrange
		var reservation = new StockReservation(Guid.NewGuid(), 1);
		reservation.Cancel("First cancel");

		// Act
		Action act = () => reservation.Cancel("Second cancel");

		// Assert
		act.Should().Throw<InvalidOperationException>();
	}

	#endregion

	#region MarkAsExpired

	[Fact]
	public void MarkAsExpired_WhenActive_ChangesStatusToExpired()
	{
		// Arrange
		var reservation = new StockReservation(Guid.NewGuid(), 1);

		// Act
		reservation.MarkAsExpired();

		// Assert
		reservation.Status.Should().Be(ReservationStatus.Expired);
		reservation.UpdatedAt.Should().NotBeNull();
	}

	[Fact]
	public void MarkAsExpired_WhenNotActive_ThrowsInvalidOperationException()
	{
		// Arrange
		var reservation = new StockReservation(Guid.NewGuid(), 1);
		reservation.Cancel();

		// Act
		Action act = () => reservation.MarkAsExpired();

		// Assert
		act.Should().Throw<InvalidOperationException>();
	}

	#endregion

	#region ExtendExpiry

	[Fact]
	public void ExtendExpiry_WhenActive_ExtendsExpiresAt()
	{
		// Arrange
		var reservation = new StockReservation(Guid.NewGuid(), 1);
		var originalExpiry = reservation.ExpiresAt;

		// Act
		reservation.ExtendExpiry(10);

		// Assert
		reservation.ExpiresAt.Should().BeCloseTo(
			originalExpiry.AddMinutes(10), TimeSpan.FromSeconds(1));
		reservation.UpdatedAt.Should().NotBeNull();
	}

	[Fact]
	public void ExtendExpiry_WhenConverted_ThrowsInvalidOperationException()
	{
		// Arrange
		var reservation = new StockReservation(Guid.NewGuid(), 1);
		reservation.ConvertToOrder(Guid.NewGuid());

		// Act
		Action act = () => reservation.ExtendExpiry(10);

		// Assert
		act.Should().Throw<InvalidOperationException>();
	}

	[Fact]
	public void ExtendExpiry_WithZeroMinutes_ThrowsArgumentOutOfRangeException()
	{
		// Arrange
		var reservation = new StockReservation(Guid.NewGuid(), 1);

		// Act
		Action act = () => reservation.ExtendExpiry(0);

		// Assert
		act.Should().Throw<ArgumentOutOfRangeException>();
	}

	#endregion

	#region IsForCart / IsForSession

	[Fact]
	public void IsForCart_WithMatchingCartId_ReturnsTrue()
	{
		// Arrange
		var cartId = Guid.NewGuid();
		var reservation = new StockReservation(Guid.NewGuid(), 1, cartId: cartId);

		// Act & Assert
		reservation.IsForCart(cartId).Should().BeTrue();
	}

	[Fact]
	public void IsForCart_WithDifferentCartId_ReturnsFalse()
	{
		// Arrange
		var reservation = new StockReservation(Guid.NewGuid(), 1, cartId: Guid.NewGuid());

		// Act & Assert
		reservation.IsForCart(Guid.NewGuid()).Should().BeFalse();
	}

	[Fact]
	public void IsForCart_WithNoCartId_ReturnsFalse()
	{
		// Arrange — session-based reservation, no cart
		var reservation = new StockReservation(Guid.NewGuid(), 1, sessionId: "s-123");

		// Act & Assert
		reservation.IsForCart(Guid.NewGuid()).Should().BeFalse();
	}

	[Fact]
	public void IsForSession_WithMatchingSessionId_ReturnsTrue()
	{
		// Arrange
		var reservation = new StockReservation(Guid.NewGuid(), 1, sessionId: "session-abc");

		// Act & Assert
		reservation.IsForSession("session-abc").Should().BeTrue();
	}

	[Fact]
	public void IsForSession_WithDifferentSessionId_ReturnsFalse()
	{
		// Arrange
		var reservation = new StockReservation(Guid.NewGuid(), 1, sessionId: "session-abc");

		// Act & Assert
		reservation.IsForSession("other-session").Should().BeFalse();
	}

	#endregion
}
