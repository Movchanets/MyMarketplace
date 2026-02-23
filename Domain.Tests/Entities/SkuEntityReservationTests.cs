using Domain.Entities;
using Domain.Enums;
using FluentAssertions;
using Xunit;

namespace Domain.Tests.Entities;

public class SkuEntityReservationTests
{
	private static SkuEntity CreateSku(decimal price = 100m, int stock = 10)
		=> SkuEntity.Create(Guid.NewGuid(), price, stock);

	#region ReserveStock

	[Fact]
	public void ReserveStock_WithSufficientStock_CreatesReservation()
	{
		// Arrange
		var sku = CreateSku(stock: 10);

		// Act
		var reservation = sku.ReserveStock(3, cartId: Guid.NewGuid());

		// Assert
		reservation.Should().NotBeNull();
		reservation.SkuId.Should().Be(sku.Id);
		reservation.Quantity.Should().Be(3);
		reservation.Status.Should().Be(ReservationStatus.Active);
		sku.ReservedQuantity.Should().Be(3);
		sku.AvailableQuantity.Should().Be(7);
		sku.StockQuantity.Should().Be(10); // Stock not yet deducted
	}

	[Fact]
	public void ReserveStock_MultipleReservations_AccumulatesReservedQuantity()
	{
		// Arrange
		var sku = CreateSku(stock: 10);

		// Act
		sku.ReserveStock(3);
		sku.ReserveStock(2);

		// Assert
		sku.ReservedQuantity.Should().Be(5);
		sku.AvailableQuantity.Should().Be(5);
		sku.StockQuantity.Should().Be(10);
	}

	[Fact]
	public void ReserveStock_ExceedsAvailableQuantity_ThrowsInvalidOperationException()
	{
		// Arrange
		var sku = CreateSku(stock: 5);
		sku.ReserveStock(3); // Available now: 2

		// Act
		Action act = () => sku.ReserveStock(3);

		// Assert
		act.Should().Throw<InvalidOperationException>()
			.WithMessage("*Insufficient available stock*");
	}

	[Fact]
	public void ReserveStock_WithZeroQuantity_ThrowsArgumentOutOfRangeException()
	{
		// Arrange
		var sku = CreateSku(stock: 10);

		// Act
		Action act = () => sku.ReserveStock(0);

		// Assert
		act.Should().Throw<ArgumentOutOfRangeException>();
	}

	[Fact]
	public void CanReserve_WithSufficientStock_ReturnsTrue()
	{
		// Arrange
		var sku = CreateSku(stock: 10);
		sku.ReserveStock(7);

		// Act & Assert
		sku.CanReserve(3).Should().BeTrue();
		sku.CanReserve(4).Should().BeFalse();
	}

	#endregion

	#region ReleaseReservation

	[Fact]
	public void ReleaseReservation_ActiveReservation_RestoresAvailability()
	{
		// Arrange
		var sku = CreateSku(stock: 10);
		var reservation = sku.ReserveStock(3);

		// Act
		sku.ReleaseReservation(reservation);

		// Assert
		sku.ReservedQuantity.Should().Be(0);
		sku.AvailableQuantity.Should().Be(10);
		sku.StockQuantity.Should().Be(10);
		reservation.Status.Should().Be(ReservationStatus.Cancelled);
	}

	[Fact]
	public void ReleaseReservation_WrongSku_ThrowsInvalidOperationException()
	{
		// Arrange
		var sku1 = CreateSku(stock: 10);
		var sku2 = CreateSku(stock: 10);
		var reservation = sku1.ReserveStock(2);

		// Act
		Action act = () => sku2.ReleaseReservation(reservation);

		// Assert
		act.Should().Throw<InvalidOperationException>()
			.WithMessage("*does not belong to this SKU*");
	}

	[Fact]
	public void ReleaseReservation_AlreadyCancelledReservation_ThrowsInvalidOperationException()
	{
		// Arrange
		var sku = CreateSku(stock: 10);
		var reservation = sku.ReserveStock(2);
		sku.ReleaseReservation(reservation); // Cancel once

		// Act — try to release again
		Action act = () => sku.ReleaseReservation(reservation);

		// Assert
		act.Should().Throw<InvalidOperationException>()
			.WithMessage("*Cannot release reservation with status*");
	}

	[Fact]
	public void ReleaseReservation_NullReservation_ThrowsArgumentNullException()
	{
		// Arrange
		var sku = CreateSku(stock: 10);

		// Act
		Action act = () => sku.ReleaseReservation(null!);

		// Assert
		act.Should().Throw<ArgumentNullException>();
	}

	#endregion

	#region ConvertReservationToDeduction

	[Fact]
	public void ConvertReservationToDeduction_ActiveReservation_DeductsStockAndConverts()
	{
		// Arrange
		var sku = CreateSku(stock: 10);
		var reservation = sku.ReserveStock(3);
		var orderId = Guid.NewGuid();

		// Act
		sku.ConvertReservationToDeduction(reservation, orderId);

		// Assert
		sku.StockQuantity.Should().Be(7);  // Deducted
		sku.ReservedQuantity.Should().Be(0);  // No longer reserved
		sku.AvailableQuantity.Should().Be(7);
		reservation.Status.Should().Be(ReservationStatus.Converted);
		reservation.OrderId.Should().Be(orderId);
	}

	[Fact]
	public void ConvertReservationToDeduction_WithMultipleReservations_OnlyConvertsSpecified()
	{
		// Arrange
		var sku = CreateSku(stock: 10);
		var reservation1 = sku.ReserveStock(3);
		var reservation2 = sku.ReserveStock(2);
		var orderId = Guid.NewGuid();

		// Act — convert only first reservation
		sku.ConvertReservationToDeduction(reservation1, orderId);

		// Assert
		sku.StockQuantity.Should().Be(7);     // 10 - 3
		sku.ReservedQuantity.Should().Be(2);   // Only reservation2 remains
		sku.AvailableQuantity.Should().Be(5);  // 7 - 2
		reservation1.Status.Should().Be(ReservationStatus.Converted);
		reservation2.Status.Should().Be(ReservationStatus.Active);
	}

	[Fact]
	public void ConvertReservationToDeduction_WrongSku_ThrowsInvalidOperationException()
	{
		// Arrange
		var sku1 = CreateSku(stock: 10);
		var sku2 = CreateSku(stock: 10);
		var reservation = sku1.ReserveStock(2);

		// Act
		Action act = () => sku2.ConvertReservationToDeduction(reservation, Guid.NewGuid());

		// Assert
		act.Should().Throw<InvalidOperationException>();
	}

	[Fact]
	public void ConvertReservationToDeduction_EmptyOrderId_ThrowsArgumentException()
	{
		// Arrange
		var sku = CreateSku(stock: 10);
		var reservation = sku.ReserveStock(2);

		// Act
		Action act = () => sku.ConvertReservationToDeduction(reservation, Guid.Empty);

		// Assert
		act.Should().Throw<ArgumentException>();
	}

	#endregion

	#region AvailableQuantity

	[Fact]
	public void AvailableQuantity_WithNoReservations_EqualsStockQuantity()
	{
		// Arrange
		var sku = CreateSku(stock: 10);

		// Act & Assert
		sku.AvailableQuantity.Should().Be(10);
		sku.ReservedQuantity.Should().Be(0);
	}

	[Fact]
	public void AvailableQuantity_AfterReserveAndRelease_RestoredCorrectly()
	{
		// Arrange
		var sku = CreateSku(stock: 10);
		var reservation = sku.ReserveStock(4);

		sku.AvailableQuantity.Should().Be(6);

		// Act
		sku.ReleaseReservation(reservation);

		// Assert
		sku.AvailableQuantity.Should().Be(10);
	}

	[Fact]
	public void AvailableQuantity_AfterConversion_ReflectsDeduction()
	{
		// Arrange
		var sku = CreateSku(stock: 10);
		var reservation = sku.ReserveStock(4);

		// Act
		sku.ConvertReservationToDeduction(reservation, Guid.NewGuid());

		// Assert
		sku.AvailableQuantity.Should().Be(6); // 10 - 4 stock, 0 reserved
		sku.StockQuantity.Should().Be(6);
		sku.ReservedQuantity.Should().Be(0);
	}

	#endregion

	#region GetActiveReservationsForCart

	[Fact]
	public void GetActiveReservationsForCart_ReturnsOnlyMatchingActiveReservations()
	{
		// Arrange
		var sku = CreateSku(stock: 20);
		var cartId = Guid.NewGuid();
		var otherCartId = Guid.NewGuid();

		var r1 = sku.ReserveStock(2, cartId: cartId);
		var r2 = sku.ReserveStock(3, cartId: otherCartId);
		var r3 = sku.ReserveStock(1, cartId: cartId);

		// Act
		var reservations = sku.GetActiveReservationsForCart(cartId).ToList();

		// Assert
		reservations.Should().HaveCount(2);
		reservations.Should().Contain(r => r.Id == r1.Id);
		reservations.Should().Contain(r => r.Id == r3.Id);
	}

	#endregion
}
