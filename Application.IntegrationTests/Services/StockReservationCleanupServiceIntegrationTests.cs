using System;
using System.Linq;
using System.Threading.Tasks;
using Domain.Entities;
using Domain.Enums;
using FluentAssertions;
using Infrastructure.Repositories;
using Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Infrastructure.IntegrationTests.Services;

public class StockReservationCleanupServiceIntegrationTests : TestBase
{
	private StockReservationCleanupService CreateService()
	{
		return new StockReservationCleanupService(
			new StockReservationRepository(DbContext),
			new SkuRepository(DbContext),
			new UnitOfWork(DbContext),
			new NullLogger<StockReservationCleanupService>());
	}

	[Fact]
	public async Task CleanupExpiredReservationsAsync_ReleasesExpiredReservation_AndUpdatesSku()
	{
		// Arrange
		var store = await CreateVerifiedStoreAsync();
		var product = await CreateProductAsync("Cleanup Test Product", "desc", store.Id, 10m, 10);
		DbContext.ChangeTracker.Clear();

		var skuId = await DbContext.Skus
			.Where(s => s.ProductId == product.Id)
			.Select(s => s.Id)
			.FirstAsync();

		var reservationId = Guid.NewGuid();
		var createdAt = DateTime.UtcNow.AddHours(-1);
		var expiresAt = DateTime.UtcNow.AddMinutes(-30);
		var statusActive = (int)ReservationStatus.Active;

		// Set reserved quantity on SKU
		await DbContext.Database.ExecuteSqlRawAsync(
			@"UPDATE ""Skus"" SET ""ReservedQuantity"" = {0} WHERE ""Id"" = {1}",
			2, skuId);

		// Insert an expired reservation (created 1h ago, expired 30min ago)
		await DbContext.Database.ExecuteSqlRawAsync(
			@"INSERT INTO ""StockReservations""
			(""Id"", ""SkuId"", ""Quantity"", ""CreatedAt"", ""ExpiresAt"", ""Status"")
			VALUES ({0}, {1}, {2}, {3}, {4}, {5})",
			reservationId, skuId, 2, createdAt, expiresAt, statusActive);

		DbContext.ChangeTracker.Clear();

		var service = CreateService();

		// Act
		var released = await service.CleanupExpiredReservationsAsync();

		// Assert
		released.Should().BeGreaterThan(0);

		DbContext.ChangeTracker.Clear();
		var updatedSku = await DbContext.Skus.FirstAsync(s => s.Id == skuId);
		updatedSku.ReservedQuantity.Should().Be(0);

		var updatedReservation = await DbContext.StockReservations.FirstAsync(r => r.Id == reservationId);
		updatedReservation.Status.Should().Be(ReservationStatus.Cancelled);
	}

	[Fact]
	public async Task CleanupExpiredReservationsAsync_NoExpired_ReturnsZero()
	{
		// Arrange — no reservations at all
		DbContext.ChangeTracker.Clear();
		var service = CreateService();

		// Act
		var released = await service.CleanupExpiredReservationsAsync();

		// Assert
		released.Should().Be(0);
	}

	[Fact]
	public async Task CleanupExpiredReservationsAsync_DoesNotTouchActiveNonExpiredReservations()
	{
		// Arrange
		var store = await CreateVerifiedStoreAsync();
		var product = await CreateProductAsync("Active Reservation Product", "desc", store.Id, 20m, 20);
		DbContext.ChangeTracker.Clear();

		var skuId = await DbContext.Skus
			.Where(s => s.ProductId == product.Id)
			.Select(s => s.Id)
			.FirstAsync();

		var activeReservationId = Guid.NewGuid();
		var createdAt = DateTime.UtcNow.AddMinutes(-5);
		var expiresAt = DateTime.UtcNow.AddMinutes(10);
		var statusActive = (int)ReservationStatus.Active;

		await DbContext.Database.ExecuteSqlRawAsync(
			@"UPDATE ""Skus"" SET ""ReservedQuantity"" = {0} WHERE ""Id"" = {1}",
			3, skuId);

		// Insert an active, non-expired reservation (expires 10min from now)
		await DbContext.Database.ExecuteSqlRawAsync(
			@"INSERT INTO ""StockReservations""
			(""Id"", ""SkuId"", ""Quantity"", ""CreatedAt"", ""ExpiresAt"", ""Status"")
			VALUES ({0}, {1}, {2}, {3}, {4}, {5})",
			activeReservationId, skuId, 3, createdAt, expiresAt, statusActive);

		DbContext.ChangeTracker.Clear();
		var service = CreateService();

		// Act
		var released = await service.CleanupExpiredReservationsAsync();

		// Assert
		released.Should().Be(0);

		DbContext.ChangeTracker.Clear();
		var reservation = await DbContext.StockReservations.FirstAsync(r => r.Id == activeReservationId);
		reservation.Status.Should().Be(ReservationStatus.Active);

		var sku = await DbContext.Skus.FirstAsync(s => s.Id == skuId);
		sku.ReservedQuantity.Should().Be(3); // Unchanged
	}

	[Fact]
	public async Task ReleaseReservationAsync_ExistingActiveReservation_ReleasesSuccessfully()
	{
		// Arrange
		var store = await CreateVerifiedStoreAsync();
		var product = await CreateProductAsync("Release Test", "desc", store.Id, 15m, 15);
		DbContext.ChangeTracker.Clear();

		var skuId = await DbContext.Skus
			.Where(s => s.ProductId == product.Id)
			.Select(s => s.Id)
			.FirstAsync();

		var reservationId = Guid.NewGuid();
		var createdAt = DateTime.UtcNow.AddMinutes(-2);
		var expiresAt = DateTime.UtcNow.AddMinutes(13);
		var statusActive = (int)ReservationStatus.Active;

		await DbContext.Database.ExecuteSqlRawAsync(
			@"UPDATE ""Skus"" SET ""ReservedQuantity"" = {0} WHERE ""Id"" = {1}",
			5, skuId);

		await DbContext.Database.ExecuteSqlRawAsync(
			@"INSERT INTO ""StockReservations""
			(""Id"", ""SkuId"", ""Quantity"", ""CreatedAt"", ""ExpiresAt"", ""Status"")
			VALUES ({0}, {1}, {2}, {3}, {4}, {5})",
			reservationId, skuId, 5, createdAt, expiresAt, statusActive);

		DbContext.ChangeTracker.Clear();
		var service = CreateService();

		// Act
		var result = await service.ReleaseReservationAsync(reservationId);

		// Assert
		result.Should().BeTrue();

		DbContext.ChangeTracker.Clear();
		var sku = await DbContext.Skus.FirstAsync(s => s.Id == skuId);
		sku.ReservedQuantity.Should().Be(0);

		var reservation = await DbContext.StockReservations.FirstAsync(r => r.Id == reservationId);
		reservation.Status.Should().Be(ReservationStatus.Cancelled);
	}

	[Fact]
	public async Task ReleaseReservationAsync_NonExistentId_ReturnsFalse()
	{
		// Arrange
		var service = CreateService();

		// Act
		var result = await service.ReleaseReservationAsync(Guid.NewGuid());

		// Assert
		result.Should().BeFalse();
	}

	[Fact]
	public async Task ReleaseReservationsForCartAsync_ReleasesAllCartReservations()
	{
		// Arrange
		var store = await CreateVerifiedStoreAsync();
		var product = await CreateProductAsync("Cart Release Test", "desc", store.Id, 25m, 25);
		DbContext.ChangeTracker.Clear();

		var skuId = await DbContext.Skus
			.Where(s => s.ProductId == product.Id)
			.Select(s => s.Id)
			.FirstAsync();

		var cartId = Guid.NewGuid();
		var reservation1Id = Guid.NewGuid();
		var reservation2Id = Guid.NewGuid();
		var createdAt1 = DateTime.UtcNow.AddMinutes(-5);
		var expiresAt1 = DateTime.UtcNow.AddMinutes(10);
		var createdAt2 = DateTime.UtcNow.AddMinutes(-3);
		var expiresAt2 = DateTime.UtcNow.AddMinutes(12);
		var statusActive = (int)ReservationStatus.Active;

		await DbContext.Database.ExecuteSqlRawAsync(
			@"UPDATE ""Skus"" SET ""ReservedQuantity"" = {0} WHERE ""Id"" = {1}",
			7, skuId);

		// Two active reservations for the same cart
		await DbContext.Database.ExecuteSqlRawAsync(
			@"INSERT INTO ""StockReservations""
			(""Id"", ""SkuId"", ""CartId"", ""Quantity"", ""CreatedAt"", ""ExpiresAt"", ""Status"")
			VALUES ({0}, {1}, {2}, {3}, {4}, {5}, {6})",
			reservation1Id, skuId, cartId, 4, createdAt1, expiresAt1, statusActive);

		await DbContext.Database.ExecuteSqlRawAsync(
			@"INSERT INTO ""StockReservations""
			(""Id"", ""SkuId"", ""CartId"", ""Quantity"", ""CreatedAt"", ""ExpiresAt"", ""Status"")
			VALUES ({0}, {1}, {2}, {3}, {4}, {5}, {6})",
			reservation2Id, skuId, cartId, 3, createdAt2, expiresAt2, statusActive);

		DbContext.ChangeTracker.Clear();
		var service = CreateService();

		// Act
		var releasedCount = await service.ReleaseReservationsForCartAsync(cartId, "Test cleanup");

		// Assert
		releasedCount.Should().Be(2);

		DbContext.ChangeTracker.Clear();
		var sku = await DbContext.Skus.FirstAsync(s => s.Id == skuId);
		sku.ReservedQuantity.Should().Be(0);

		var reservations = await DbContext.StockReservations
			.Where(r => r.CartId == cartId)
			.ToListAsync();
		reservations.Should().AllSatisfy(r => r.Status.Should().Be(ReservationStatus.Cancelled));
	}

	[Fact]
	public async Task ExtendReservationAsync_ActiveReservation_ExtendsExpiry()
	{
		// Arrange
		var store = await CreateVerifiedStoreAsync();
		var product = await CreateProductAsync("Extend Test", "desc", store.Id, 10m, 10);
		DbContext.ChangeTracker.Clear();

		var skuId = await DbContext.Skus
			.Where(s => s.ProductId == product.Id)
			.Select(s => s.Id)
			.FirstAsync();

		var reservationId = Guid.NewGuid();
		var createdAt = DateTime.UtcNow.AddMinutes(-10);
		var originalExpiry = DateTime.UtcNow.AddMinutes(5);
		var statusActive = (int)ReservationStatus.Active;

		await DbContext.Database.ExecuteSqlRawAsync(
			@"UPDATE ""Skus"" SET ""ReservedQuantity"" = {0} WHERE ""Id"" = {1}",
			2, skuId);

		await DbContext.Database.ExecuteSqlRawAsync(
			@"INSERT INTO ""StockReservations""
			(""Id"", ""SkuId"", ""Quantity"", ""CreatedAt"", ""ExpiresAt"", ""Status"")
			VALUES ({0}, {1}, {2}, {3}, {4}, {5})",
			reservationId, skuId, 2, createdAt, originalExpiry, statusActive);

		DbContext.ChangeTracker.Clear();
		var service = CreateService();

		// Act
		var result = await service.ExtendReservationAsync(reservationId, 10);

		// Assert
		result.Should().BeTrue();

		DbContext.ChangeTracker.Clear();
		var reservation = await DbContext.StockReservations.FirstAsync(r => r.Id == reservationId);
		reservation.ExpiresAt.Should().BeCloseTo(originalExpiry.AddMinutes(10), TimeSpan.FromSeconds(2));
		reservation.Status.Should().Be(ReservationStatus.Active);
	}

	#region Helpers

	private async Task<Store> CreateVerifiedStoreAsync()
	{
		var identityUser = new Infrastructure.Entities.Identity.ApplicationUser
		{
			Email = $"owner_{Guid.NewGuid()}@test.com",
			UserName = $"owner_{Guid.NewGuid()}",
			EmailConfirmed = true
		};
		await UserManager.CreateAsync(identityUser, "Test123!");

		var domainUser = new User(identityUser.Id, "Store", "Owner", identityUser.Email);
		typeof(BaseEntity<Guid>).GetProperty(nameof(BaseEntity<Guid>.Id))!
			.SetValue(domainUser, Guid.NewGuid());
		UserRepository.Add(domainUser);
		await DbContext.SaveChangesAsync();

		var store = Store.Create(domainUser.Id, "Test Store", "Test store description");
		store.Verify();
		DbContext.Stores.Add(store);
		await DbContext.SaveChangesAsync();

		return store;
	}

	private async Task<Product> CreateProductAsync(string name, string description, Guid storeId, decimal price, int stock)
	{
		var product = new Product(name, description);
		product.Activate();
		DbContext.Products.Add(product);
		await DbContext.SaveChangesAsync();

		DbContext.Entry(product).Property("StoreId").CurrentValue = storeId;

		var sku = SkuEntity.Create(product.Id, price, stock);
		DbContext.Skus.Add(sku);
		await DbContext.SaveChangesAsync();

		return product;
	}

	#endregion
}

