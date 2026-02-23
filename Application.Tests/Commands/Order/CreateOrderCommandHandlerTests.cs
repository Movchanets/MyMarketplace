using Application.Commands.Order.CreateOrder;
using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;
using Domain.Interfaces.Repositories;
using Domain.ValueObjects;
using FluentAssertions;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using Moq;
using System.Data;
using Xunit;

namespace Application.Tests.Commands.Order;

// Aliases to resolve namespace conflicts (e.g. Application.Tests.Commands.User namespace vs Domain.Entities.User type)
using DomainUser = global::Domain.Entities.User;
using DomainProduct = global::Domain.Entities.Product;
using DomainCart = global::Domain.Entities.Cart;
using DomainOrder = global::Domain.Entities.Order;

public class CreateOrderCommandHandlerTests
{
	private readonly Mock<IOrderRepository> _orderRepo = new();
	private readonly Mock<ICartRepository> _cartRepo = new();
	private readonly Mock<ISkuRepository> _skuRepo = new();
	private readonly Mock<IUserRepository> _userRepo = new();
	private readonly Mock<IStockReservationRepository> _stockReservationRepo = new();
	private readonly Mock<IUnitOfWork> _unitOfWork = new();
	private readonly Mock<ILogger<CreateOrderCommandHandler>> _logger = new();
	private readonly Mock<IDbContextTransaction> _transaction = new();

	private CreateOrderCommandHandler CreateSut()
		=> new(
			_orderRepo.Object,
			_cartRepo.Object,
			_skuRepo.Object,
			_userRepo.Object,
			_stockReservationRepo.Object,
			_unitOfWork.Object,
			_logger.Object);

	public CreateOrderCommandHandlerTests()
	{
		// Default: transaction always available
		_unitOfWork
			.Setup(x => x.BeginTransactionAsync(It.IsAny<IsolationLevel>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync(_transaction.Object);

		// Default: no active reservations
		_stockReservationRepo
			.Setup(x => x.GetActiveByCartIdTrackedAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync(Enumerable.Empty<StockReservation>());
	}

	#region Helpers

	private static ShippingAddress CreateAddress() => new(
		"John", "Doe", "+380991234567", "john@example.com",
		"вул. Хрещатик 1", null, "Київ", null, "01001", "Україна");

	private static CreateOrderCommand CreateCommand(Guid userId, string? idempotencyKey = null) => new(
		userId,
		CreateAddress(),
		"nova_poshta",
		"card",
		null,
		null,
		idempotencyKey);

	private static DomainUser CreateDomainUser(Guid identityUserId)
	{
		var user = new DomainUser(identityUserId, "John", "Doe", "john@example.com");
		// User constructor doesn't set BaseEntity.Id — set it via reflection (EF normally does this)
		typeof(BaseEntity<Guid>).GetProperty("Id")!.SetValue(user, Guid.NewGuid());
		return user;
	}

	private static DomainProduct CreateProduct(string name = "Test Product")
		=> new(name, "Test description");

	private static SkuEntity CreateSku(Guid productId, decimal price = 100m, int stock = 10)
		=> SkuEntity.Create(productId, price, stock);

	/// <summary>
	/// Creates a Cart with CartItems that have Product navigation property set via reflection.
	/// This simulates what EF Core does when using GetByUserIdWithProductsAsync (Include Product + SKU).
	/// </summary>
	private static DomainCart CreateCartWithItems(Guid userId, params (DomainProduct product, SkuEntity sku, int qty)[] items)
	{
		var cart = new DomainCart(userId);

		foreach (var (product, sku, qty) in items)
		{
			cart.AddItem(product.Id, sku.Id, qty);
		}

		// Set Product navigation property on each CartItem via reflection
		// (EF Core normally does this during materialization)
		var cartItems = cart.Items.ToList();
		for (int i = 0; i < cartItems.Count; i++)
		{
			var cartItem = cartItems[i];
			var productProp = typeof(CartItem).GetProperty("Product");
			productProp!.SetValue(cartItem, items[i].product);
		}

		return cart;
	}

	private void SetupUserFound(Guid identityUserId, DomainUser domainUser)
	{
		_userRepo
			.Setup(x => x.GetByIdentityUserIdAsync(identityUserId))
			.ReturnsAsync(domainUser);
	}

	private void SetupCartWithProducts(Guid domainUserId, DomainCart cart)
	{
		_cartRepo
			.Setup(x => x.GetByUserIdWithProductsAsync(domainUserId, It.IsAny<CancellationToken>()))
			.ReturnsAsync(cart);
	}

	private void SetupSku(SkuEntity sku)
	{
		_skuRepo
			.Setup(x => x.GetByIdAsync(sku.Id))
			.ReturnsAsync(sku);
	}

	#endregion

	#region Idempotency

	[Fact]
	public async Task Handle_WithExistingIdempotencyKey_ReturnsExistingOrder()
	{
		// Arrange
		var identityUserId = Guid.NewGuid();
		var idempotencyKey = "idem-123";
		var command = CreateCommand(identityUserId, idempotencyKey);

		var existingOrder = new DomainOrder(
			Guid.NewGuid(), CreateAddress(), "nova_poshta", "card", idempotencyKey);

		_orderRepo
			.Setup(x => x.GetByIdempotencyKeyAsync(idempotencyKey, It.IsAny<CancellationToken>()))
			.ReturnsAsync(existingOrder);

		var sut = CreateSut();

		// Act
		var result = await sut.Handle(command, CancellationToken.None);

		// Assert
		result.IsSuccess.Should().BeTrue();
		result.Message.Should().Contain("already exists");
		result.Payload.Should().NotBeNull();
		result.Payload!.Id.Should().Be(existingOrder.Id);

		// Should NOT start a transaction when returning cached order
		_unitOfWork.Verify(x => x.BeginTransactionAsync(
			It.IsAny<IsolationLevel>(), It.IsAny<CancellationToken>()), Times.Never);
	}

	[Fact]
	public async Task Handle_WithoutIdempotencyKey_DoesNotCheckForExistingOrder()
	{
		// Arrange
		var identityUserId = Guid.NewGuid();
		var command = CreateCommand(identityUserId, idempotencyKey: null);

		_userRepo.Setup(x => x.GetByIdentityUserIdAsync(identityUserId)).ReturnsAsync((DomainUser?)null);

		var sut = CreateSut();

		// Act
		await sut.Handle(command, CancellationToken.None);

		// Assert
		_orderRepo.Verify(x => x.GetByIdempotencyKeyAsync(
			It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
	}

	#endregion

	#region User validation

	[Fact]
	public async Task Handle_WhenUserNotFound_ReturnsError()
	{
		// Arrange
		var identityUserId = Guid.NewGuid();
		var command = CreateCommand(identityUserId);

		_userRepo.Setup(x => x.GetByIdentityUserIdAsync(identityUserId)).ReturnsAsync((DomainUser?)null);

		var sut = CreateSut();

		// Act
		var result = await sut.Handle(command, CancellationToken.None);

		// Assert
		result.IsSuccess.Should().BeFalse();
		result.Message.Should().Be("User not found");
	}

	#endregion

	#region Cart validation

	[Fact]
	public async Task Handle_WhenCartIsNull_ReturnsEmptyCartError()
	{
		// Arrange
		var identityUserId = Guid.NewGuid();
		var command = CreateCommand(identityUserId);
		var domainUser = CreateDomainUser(identityUserId);

		SetupUserFound(identityUserId, domainUser);
		_cartRepo
			.Setup(x => x.GetByUserIdWithProductsAsync(domainUser.Id, It.IsAny<CancellationToken>()))
			.ReturnsAsync((DomainCart?)null);

		var sut = CreateSut();

		// Act
		var result = await sut.Handle(command, CancellationToken.None);

		// Assert
		result.IsSuccess.Should().BeFalse();
		result.Message.Should().Be("Cart is empty");
	}

	[Fact]
	public async Task Handle_WhenCartHasNoItems_ReturnsEmptyCartError()
	{
		// Arrange
		var identityUserId = Guid.NewGuid();
		var command = CreateCommand(identityUserId);
		var domainUser = CreateDomainUser(identityUserId);
		var emptyCart = new DomainCart(domainUser.Id);

		SetupUserFound(identityUserId, domainUser);
		SetupCartWithProducts(domainUser.Id, emptyCart);

		var sut = CreateSut();

		// Act
		var result = await sut.Handle(command, CancellationToken.None);

		// Assert
		result.IsSuccess.Should().BeFalse();
		result.Message.Should().Be("Cart is empty");
	}

	#endregion

	#region SKU validation (Bug fix: previously returned no error)

	[Fact]
	public async Task Handle_WhenSkuNotFound_ReturnsError()
	{
		// Arrange
		var identityUserId = Guid.NewGuid();
		var command = CreateCommand(identityUserId);
		var domainUser = CreateDomainUser(identityUserId);

		var product = CreateProduct();
		var sku = CreateSku(product.Id);
		var cart = CreateCartWithItems(domainUser.Id, (product, sku, 2));

		SetupUserFound(identityUserId, domainUser);
		SetupCartWithProducts(domainUser.Id, cart);

		// SKU not found in repository
		_skuRepo.Setup(x => x.GetByIdAsync(sku.Id)).ReturnsAsync((SkuEntity?)null);

		var sut = CreateSut();

		// Act
		var result = await sut.Handle(command, CancellationToken.None);

		// Assert
		result.IsSuccess.Should().BeFalse();
		result.Message.Should().Contain("Product variant not found");
		result.Message.Should().Contain(sku.Id.ToString());
	}

	#endregion

	#region Product validation (Bug fix: previously silently skipped)

	[Fact]
	public async Task Handle_WhenProductNotLoaded_ReturnsError()
	{
		// Arrange — simulate a scenario where EF failed to load the Product navigation property
		var identityUserId = Guid.NewGuid();
		var command = CreateCommand(identityUserId);
		var domainUser = CreateDomainUser(identityUserId);

		var product = CreateProduct();
		var sku = CreateSku(product.Id, stock: 10);

		// Create cart WITHOUT setting Product navigation
		var cart = new DomainCart(domainUser.Id);
		cart.AddItem(product.Id, sku.Id, 2);
		// NOTE: Not setting Product on CartItem — it stays null

		SetupUserFound(identityUserId, domainUser);
		SetupCartWithProducts(domainUser.Id, cart);
		SetupSku(sku);

		var sut = CreateSut();

		// Act
		var result = await sut.Handle(command, CancellationToken.None);

		// Assert
		result.IsSuccess.Should().BeFalse();
		result.Message.Should().Contain("Product not found for cart item");
	}

	#endregion

	#region Stock validation

	[Fact]
	public async Task Handle_WhenInsufficientStock_ReturnsError()
	{
		// Arrange
		var identityUserId = Guid.NewGuid();
		var command = CreateCommand(identityUserId);
		var domainUser = CreateDomainUser(identityUserId);

		var product = CreateProduct();
		var sku = CreateSku(product.Id, stock: 1); // Only 1 in stock
		var cart = CreateCartWithItems(domainUser.Id, (product, sku, 5)); // Requesting 5

		SetupUserFound(identityUserId, domainUser);
		SetupCartWithProducts(domainUser.Id, cart);
		SetupSku(sku);

		var sut = CreateSut();

		// Act
		var result = await sut.Handle(command, CancellationToken.None);

		// Assert
		result.IsSuccess.Should().BeFalse();
		result.Message.Should().Contain("Insufficient stock");
		result.Message.Should().Contain(sku.SkuCode);
	}

	#endregion

	#region Successful order creation

	[Fact]
	public async Task Handle_WithValidData_CreatesOrderSuccessfully()
	{
		// Arrange
		var identityUserId = Guid.NewGuid();
		var command = CreateCommand(identityUserId);
		var domainUser = CreateDomainUser(identityUserId);

		var product = CreateProduct("iPhone 15");
		var sku = CreateSku(product.Id, price: 999m, stock: 10);
		var cart = CreateCartWithItems(domainUser.Id, (product, sku, 2));

		SetupUserFound(identityUserId, domainUser);
		SetupCartWithProducts(domainUser.Id, cart);
		SetupSku(sku);

		var sut = CreateSut();

		// Act
		var result = await sut.Handle(command, CancellationToken.None);

		// Assert
		result.IsSuccess.Should().BeTrue();
		result.Payload.Should().NotBeNull();
		result.Payload!.Items.Should().HaveCount(1);
		result.Payload.Items[0].Quantity.Should().Be(2);
		result.Payload.Items[0].PriceAtPurchase.Should().Be(999m);
		result.Payload.Items[0].ProductName.Should().Be("iPhone 15");
	}

	[Fact]
	public async Task Handle_WithValidData_DeductsStock()
	{
		// Arrange
		var identityUserId = Guid.NewGuid();
		var command = CreateCommand(identityUserId);
		var domainUser = CreateDomainUser(identityUserId);

		var product = CreateProduct();
		var sku = CreateSku(product.Id, stock: 10);
		var cart = CreateCartWithItems(domainUser.Id, (product, sku, 3));

		SetupUserFound(identityUserId, domainUser);
		SetupCartWithProducts(domainUser.Id, cart);
		SetupSku(sku);

		var sut = CreateSut();

		// Act
		await sut.Handle(command, CancellationToken.None);

		// Assert — stock should be deducted from 10 to 7
		sku.StockQuantity.Should().Be(7);
		_skuRepo.Verify(x => x.Update(sku), Times.Once);
	}

	[Fact]
	public async Task Handle_WithValidData_ClearsCart()
	{
		// Arrange
		var identityUserId = Guid.NewGuid();
		var command = CreateCommand(identityUserId);
		var domainUser = CreateDomainUser(identityUserId);

		var product = CreateProduct();
		var sku = CreateSku(product.Id, stock: 10);
		var cart = CreateCartWithItems(domainUser.Id, (product, sku, 1));

		SetupUserFound(identityUserId, domainUser);
		SetupCartWithProducts(domainUser.Id, cart);
		SetupSku(sku);

		var sut = CreateSut();

		// Act
		await sut.Handle(command, CancellationToken.None);

		// Assert — cart should be cleared after order creation
		cart.Items.Should().BeEmpty();
	}

	[Fact]
	public async Task Handle_WithValidData_SavesAndCommitsTransaction()
	{
		// Arrange
		var identityUserId = Guid.NewGuid();
		var command = CreateCommand(identityUserId);
		var domainUser = CreateDomainUser(identityUserId);

		var product = CreateProduct();
		var sku = CreateSku(product.Id, stock: 10);
		var cart = CreateCartWithItems(domainUser.Id, (product, sku, 1));

		SetupUserFound(identityUserId, domainUser);
		SetupCartWithProducts(domainUser.Id, cart);
		SetupSku(sku);

		var sut = CreateSut();

		// Act
		await sut.Handle(command, CancellationToken.None);

		// Assert
		_orderRepo.Verify(x => x.Add(It.IsAny<DomainOrder>()), Times.Once);
		_unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
		_transaction.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
	}

	[Fact]
	public async Task Handle_WithMultipleItems_CreatesAllOrderItems()
	{
		// Arrange
		var identityUserId = Guid.NewGuid();
		var command = CreateCommand(identityUserId);
		var domainUser = CreateDomainUser(identityUserId);

		var product1 = CreateProduct("Product A");
		var sku1 = CreateSku(product1.Id, price: 100m, stock: 10);

		var product2 = CreateProduct("Product B");
		var sku2 = CreateSku(product2.Id, price: 200m, stock: 5);

		var cart = CreateCartWithItems(domainUser.Id,
			(product1, sku1, 2),
			(product2, sku2, 1));

		SetupUserFound(identityUserId, domainUser);
		SetupCartWithProducts(domainUser.Id, cart);
		SetupSku(sku1);
		SetupSku(sku2);

		var sut = CreateSut();

		// Act
		var result = await sut.Handle(command, CancellationToken.None);

		// Assert
		result.IsSuccess.Should().BeTrue();
		result.Payload!.Items.Should().HaveCount(2);

		// Total: 2 * 100 + 1 * 200 = 400
		result.Payload.TotalPrice.Should().Be(400m);

		// Both SKUs should be updated
		_skuRepo.Verify(x => x.Update(sku1), Times.Once);
		_skuRepo.Verify(x => x.Update(sku2), Times.Once);
	}

	#endregion

	#region Transaction isolation (Bug fix: RepeatableRead)

	[Fact]
	public async Task Handle_UsesRepeatableReadIsolation()
	{
		// Arrange
		var identityUserId = Guid.NewGuid();
		var command = CreateCommand(identityUserId);
		var domainUser = CreateDomainUser(identityUserId);

		var product = CreateProduct();
		var sku = CreateSku(product.Id, stock: 10);
		var cart = CreateCartWithItems(domainUser.Id, (product, sku, 1));

		SetupUserFound(identityUserId, domainUser);
		SetupCartWithProducts(domainUser.Id, cart);
		SetupSku(sku);

		var sut = CreateSut();

		// Act
		await sut.Handle(command, CancellationToken.None);

		// Assert — should use RepeatableRead, not default ReadCommitted
		_unitOfWork.Verify(x => x.BeginTransactionAsync(
			IsolationLevel.RepeatableRead, It.IsAny<CancellationToken>()), Times.Once);
	}

	#endregion

	#region Stock reservations (Bug fix: dead reservation code)

	[Fact]
	public async Task Handle_ConvertsActiveReservationsToOrder()
	{
		// Arrange
		var identityUserId = Guid.NewGuid();
		var command = CreateCommand(identityUserId);
		var domainUser = CreateDomainUser(identityUserId);

		var product = CreateProduct();
		var sku = CreateSku(product.Id, stock: 10);
		var cart = CreateCartWithItems(domainUser.Id, (product, sku, 2));

		// Create an active reservation for this cart + SKU
		var reservation = new StockReservation(sku.Id, 2, cart.Id);

		SetupUserFound(identityUserId, domainUser);
		SetupCartWithProducts(domainUser.Id, cart);
		SetupSku(sku);

		_stockReservationRepo
			.Setup(x => x.GetActiveByCartIdTrackedAsync(cart.Id, It.IsAny<CancellationToken>()))
			.ReturnsAsync(new List<StockReservation> { reservation });

		var sut = CreateSut();

		// Act
		var result = await sut.Handle(command, CancellationToken.None);

		// Assert
		result.IsSuccess.Should().BeTrue();

		// Reservation should have been converted via sku.ConvertReservationToDeduction
		// which sets Status = Converted and OrderId on the tracked entity
		reservation.OrderId.Should().NotBeNull();
		reservation.Status.Should().Be(Domain.Enums.ReservationStatus.Converted);

		// Stock should be deducted via ConvertReservationToDeduction (not DeductStock)
		// 10 original - 2 converted = 8
		sku.StockQuantity.Should().Be(8);
		sku.ReservedQuantity.Should().Be(0); // Reservation was converted, not just released
	}

	[Fact]
	public async Task Handle_WithNoReservations_StillSucceeds()
	{
		// Arrange
		var identityUserId = Guid.NewGuid();
		var command = CreateCommand(identityUserId);
		var domainUser = CreateDomainUser(identityUserId);

		var product = CreateProduct();
		var sku = CreateSku(product.Id, stock: 10);
		var cart = CreateCartWithItems(domainUser.Id, (product, sku, 1));

		SetupUserFound(identityUserId, domainUser);
		SetupCartWithProducts(domainUser.Id, cart);
		SetupSku(sku);

		// No reservations (empty list — default mock already set)

		var sut = CreateSut();

		// Act
		var result = await sut.Handle(command, CancellationToken.None);

		// Assert
		result.IsSuccess.Should().BeTrue();
	}

	#endregion

	#region Customer notes

	[Fact]
	public async Task Handle_WithCustomerNotes_SetsNotesOnOrder()
	{
		// Arrange
		var identityUserId = Guid.NewGuid();
		var domainUser = CreateDomainUser(identityUserId);
		var product = CreateProduct();
		var sku = CreateSku(product.Id, stock: 10);
		var cart = CreateCartWithItems(domainUser.Id, (product, sku, 1));

		var command = new CreateOrderCommand(
			identityUserId,
			CreateAddress(),
			"nova_poshta",
			"card",
			null,
			"Please leave at door",
			null);

		SetupUserFound(identityUserId, domainUser);
		SetupCartWithProducts(domainUser.Id, cart);
		SetupSku(sku);

		DomainOrder? capturedOrder = null;
		_orderRepo
			.Setup(x => x.Add(It.IsAny<DomainOrder>()))
			.Callback<DomainOrder>(o => capturedOrder = o);

		var sut = CreateSut();

		// Act
		await sut.Handle(command, CancellationToken.None);

		// Assert
		capturedOrder.Should().NotBeNull();
		capturedOrder!.CustomerNotes.Should().Be("Please leave at door");
	}

	#endregion

	#region Error handling & rollback

	[Fact]
	public async Task Handle_WhenExceptionOccurs_RollsBackTransaction()
	{
		// Arrange
		var identityUserId = Guid.NewGuid();
		var command = CreateCommand(identityUserId);
		var domainUser = CreateDomainUser(identityUserId);

		var product = CreateProduct();
		var sku = CreateSku(product.Id, stock: 10);
		var cart = CreateCartWithItems(domainUser.Id, (product, sku, 1));

		SetupUserFound(identityUserId, domainUser);
		SetupCartWithProducts(domainUser.Id, cart);
		SetupSku(sku);

		// Make SaveChangesAsync throw
		_unitOfWork
			.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
			.ThrowsAsync(new InvalidOperationException("DB error"));

		var sut = CreateSut();

		// Act
		var result = await sut.Handle(command, CancellationToken.None);

		// Assert
		result.IsSuccess.Should().BeFalse();
		result.Message.Should().Contain("error occurred");
		_transaction.Verify(x => x.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
		_transaction.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
	}

	#endregion

	#region SKU pre-fetch optimization (Bug fix: double fetch)

	[Fact]
	public async Task Handle_FetchesEachSkuExactlyOnce()
	{
		// Arrange — verify the fix that eliminated double SKU fetching
		var identityUserId = Guid.NewGuid();
		var command = CreateCommand(identityUserId);
		var domainUser = CreateDomainUser(identityUserId);

		var product = CreateProduct();
		var sku = CreateSku(product.Id, stock: 10);
		var cart = CreateCartWithItems(domainUser.Id, (product, sku, 2));

		SetupUserFound(identityUserId, domainUser);
		SetupCartWithProducts(domainUser.Id, cart);
		SetupSku(sku);

		var sut = CreateSut();

		// Act
		await sut.Handle(command, CancellationToken.None);

		// Assert — GetByIdAsync should be called exactly once per SKU
		_skuRepo.Verify(x => x.GetByIdAsync(sku.Id), Times.Once);
	}

	#endregion

	#region Cart loaded with products (Bug fix: GetByUserIdWithProductsAsync)

	[Fact]
	public async Task Handle_LoadsCartWithProductsNotJustItems()
	{
		// Arrange — verify the fix uses GetByUserIdWithProductsAsync instead of GetByUserIdAsync
		var identityUserId = Guid.NewGuid();
		var command = CreateCommand(identityUserId);
		var domainUser = CreateDomainUser(identityUserId);

		var product = CreateProduct();
		var sku = CreateSku(product.Id, stock: 10);
		var cart = CreateCartWithItems(domainUser.Id, (product, sku, 1));

		SetupUserFound(identityUserId, domainUser);
		SetupCartWithProducts(domainUser.Id, cart);
		SetupSku(sku);

		var sut = CreateSut();

		// Act
		await sut.Handle(command, CancellationToken.None);

		// Assert — must use GetByUserIdWithProductsAsync, NOT GetByUserIdAsync
		_cartRepo.Verify(
			x => x.GetByUserIdWithProductsAsync(domainUser.Id, It.IsAny<CancellationToken>()),
			Times.Once);
		_cartRepo.Verify(
			x => x.GetByUserIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
			Times.Never);
	}

	#endregion

	#region Order DTO mapping

	[Fact]
	public async Task Handle_ReturnsCorrectlyMappedDto()
	{
		// Arrange
		var identityUserId = Guid.NewGuid();
		var command = CreateCommand(identityUserId);
		var domainUser = CreateDomainUser(identityUserId);

		var product = CreateProduct("MacBook Pro");
		var sku = CreateSku(product.Id, price: 2499m, stock: 5);
		var cart = CreateCartWithItems(domainUser.Id, (product, sku, 1));

		SetupUserFound(identityUserId, domainUser);
		SetupCartWithProducts(domainUser.Id, cart);
		SetupSku(sku);

		var sut = CreateSut();

		// Act
		var result = await sut.Handle(command, CancellationToken.None);

		// Assert
		result.Payload.Should().NotBeNull();
		var dto = result.Payload!;
		dto.Id.Should().NotBeEmpty();
		dto.OrderNumber.Should().StartWith("ORD-");
		dto.UserId.Should().Be(domainUser.Id);
		dto.TotalPrice.Should().Be(2499m);
		dto.Status.Should().Be(Domain.Enums.OrderStatus.Pending);
		dto.PaymentStatus.Should().Be(Domain.Enums.PaymentStatus.Pending);
		dto.DeliveryMethod.Should().Be("nova_poshta");
		dto.PaymentMethod.Should().Be("card");

		var item = dto.Items.Should().ContainSingle().Subject;
		item.ProductName.Should().Be("MacBook Pro");
		item.PriceAtPurchase.Should().Be(2499m);
		item.Quantity.Should().Be(1);
		item.Subtotal.Should().Be(2499m);
		item.SkuCode.Should().NotBeNullOrEmpty();
	}

	#endregion
}
