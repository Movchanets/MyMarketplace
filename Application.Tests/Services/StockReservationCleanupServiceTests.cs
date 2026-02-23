using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Domain.Entities;
using Domain.Enums;
using FluentAssertions;
using Infrastructure.Services;
using Moq;
using Domain.Interfaces.Repositories;
using Application.Interfaces;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Application.Tests.Services
{
	public class StockReservationCleanupServiceTests
	{
		[Fact]
		public async Task CleanupExpiredReservationsAsync_NoExpiredReservations_ReturnsZero()
		{
			// Arrange
			var mockReservationRepo = new Mock<IStockReservationRepository>();
			mockReservationRepo
				.Setup(r => r.GetExpiredReservationsTrackedAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
				.ReturnsAsync(Enumerable.Empty<StockReservation>());

			var mockSkuRepo = new Mock<ISkuRepository>();
			var mockUnit = new Mock<IUnitOfWork>();
			var mockLogger = new Mock<ILogger<StockReservationCleanupService>>();

			var service = new StockReservationCleanupService(
				mockReservationRepo.Object,
				mockSkuRepo.Object,
				mockUnit.Object,
				mockLogger.Object);

			// Act
			var result = await service.CleanupExpiredReservationsAsync();

			// Assert
			result.Should().Be(0);
		}

		[Fact]
		public async Task ReleaseReservationAsync_NotFound_ReturnsFalse()
		{
			// Arrange
			var mockReservationRepo = new Mock<IStockReservationRepository>();
			mockReservationRepo
				.Setup(r => r.GetByIdTrackedAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
				.ReturnsAsync((StockReservation?)null);

			var mockSkuRepo = new Mock<ISkuRepository>();
			var mockUnit = new Mock<IUnitOfWork>();
			var mockLogger = new Mock<ILogger<StockReservationCleanupService>>();

			var service = new StockReservationCleanupService(
				mockReservationRepo.Object,
				mockSkuRepo.Object,
				mockUnit.Object,
				mockLogger.Object);

			// Act
			var result = await service.ReleaseReservationAsync(Guid.NewGuid());

			// Assert
			result.Should().BeFalse();
		}

		[Fact]
		public async Task CleanupExpiredReservationsAsync_ExpiredReservation_ReleasesStockAndSaves()
		{
			// Arrange
			var sku = SkuEntity.Create(Guid.NewGuid(), 10m, 5);
			var reservation = sku.ReserveStock(1);

			// Make reservation expired via reflection (ExpiresAt has private setter)
			var expiresAtProp = typeof(StockReservation).GetProperty("ExpiresAt");
			expiresAtProp!.SetValue(reservation, DateTime.UtcNow.AddMinutes(-5));

			var mockReservationRepo = new Mock<IStockReservationRepository>();
			mockReservationRepo
				.Setup(r => r.GetExpiredReservationsTrackedAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
				.ReturnsAsync(new[] { reservation });

			var mockSkuRepo = new Mock<ISkuRepository>();
			mockSkuRepo.Setup(s => s.GetByIdAsync(sku.Id)).ReturnsAsync(sku);
			mockSkuRepo.Setup(s => s.Update(It.IsAny<SkuEntity>()));

			var mockUnit = new Mock<IUnitOfWork>();
			// Setup ExecuteInTransactionAsync to actually execute the callback
			mockUnit
				.Setup(u => u.ExecuteInTransactionAsync(It.IsAny<Func<CancellationToken, Task<bool>>>(), It.IsAny<CancellationToken>()))
				.Returns<Func<CancellationToken, Task<bool>>, CancellationToken>(async (func, ct) => await func(ct));

			var mockLogger = new Mock<ILogger<StockReservationCleanupService>>();

			var service = new StockReservationCleanupService(
				mockReservationRepo.Object,
				mockSkuRepo.Object,
				mockUnit.Object,
				mockLogger.Object);

			// Act
			var result = await service.CleanupExpiredReservationsAsync();

			// Assert
			result.Should().Be(1);
			sku.ReservedQuantity.Should().Be(0);
			reservation.Status.Should().Be(ReservationStatus.Cancelled);
			// Note: Update() is not called on tracked entities - EF Core tracks changes automatically
		}
	}
}
