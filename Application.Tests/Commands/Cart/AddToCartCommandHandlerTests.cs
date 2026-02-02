using Application.Commands.Cart.AddToCart;
using Application.Interfaces;
using Domain.Entities;
using Domain.Interfaces.Repositories;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using UserEntity = Domain.Entities.User;

namespace Application.Tests.Commands.Cart;

public class AddToCartCommandHandlerTests
{
	private readonly Mock<ICartService> _cartService = new();
	private readonly Mock<ICartRepository> _cartRepository = new();
	private readonly Mock<IProductRepository> _productRepository = new();
	private readonly Mock<ISkuRepository> _skuRepository = new();
	private readonly Mock<IUnitOfWork> _unitOfWork = new();
	private readonly Mock<ILogger<AddToCartCommandHandler>> _logger = new();

	private AddToCartCommandHandler CreateSut()
		=> new(
			_cartService.Object,
			_cartRepository.Object,
			_productRepository.Object,
			_skuRepository.Object,
			_unitOfWork.Object,
			_logger.Object);

	private void SetupCartServiceForSuccess(Guid userId, Guid domainUserId, Domain.Entities.Cart? cart = null)
	{
		var domainUser = CreateDomainUser(userId, domainUserId);
		var isNewCart = cart is null;
		cart ??= new Domain.Entities.Cart(domainUserId);

		_cartService.Setup(x => x.GetOrCreateCartAsync(userId, It.IsAny<CancellationToken>()))
			.ReturnsAsync(CartOperationResult<(Domain.Entities.Cart, Domain.Entities.User, bool)>.Success((cart, domainUser, isNewCart)));

		_cartService.Setup(x => x.ValidateTotalQuantity(It.IsAny<int>(), It.IsAny<int>()))
			.Returns((int requested, int existing) => CartOperationResult<int>.Success(requested + existing));


		_cartService.Setup(x => x.MapToCartDto(It.IsAny<Domain.Entities.Cart>()))
			.Returns((Domain.Entities.Cart c) => new CartDto(
				c.Id,
				c.UserId,
				new List<CartItemDto>(),
				c.GetTotalItems(),
				0m));
	}

	private static UserEntity CreateDomainUser(Guid identityUserId, Guid domainUserId)
	{
		var user = new UserEntity(identityUserId, email: $"user_{identityUserId:N}@example.com");
		typeof(Domain.Entities.BaseEntity<Guid>)
			.GetProperty(nameof(Domain.Entities.BaseEntity<Guid>.Id))
			?.SetValue(user, domainUserId);
		return user;
	}

	[Fact]
	public async Task Handle_WhenUserNotFound_ReturnsError()
	{
		// Arrange
		var sut = CreateSut();
		var identityUserId = Guid.NewGuid();
		var command = new AddToCartCommand(identityUserId, Guid.NewGuid(), Guid.NewGuid(), 1);

		// ProductRepository returns null to trigger "Product not found" before cart service is called
		_productRepository.Setup(x => x.GetByIdAsync(It.IsAny<Guid>()))
			.ReturnsAsync((Domain.Entities.Product?)null);

		// Act
		var result = await sut.Handle(command, CancellationToken.None);

		// Assert
		result.IsSuccess.Should().BeFalse();
		result.Message.Should().Be("Product not found");
	}

	[Fact]
	public async Task Handle_WhenQuantityIsZero_ReturnsError()
	{
		// Arrange
		var sut = CreateSut();
		var identityUserId = Guid.NewGuid();
		var command = new AddToCartCommand(identityUserId, Guid.NewGuid(), Guid.NewGuid(), 0);

		// Act
		var result = await sut.Handle(command, CancellationToken.None);

		// Assert
		result.IsSuccess.Should().BeFalse();
		result.Message.Should().Be("Quantity must be greater than zero");
	}

	[Fact]
	public async Task Handle_WhenProductNotFound_ReturnsError()
	{
		// Arrange
		var sut = CreateSut();
		var identityUserId = Guid.NewGuid();
		var productId = Guid.NewGuid();
		var command = new AddToCartCommand(identityUserId, productId, Guid.NewGuid(), 1);

		_productRepository.Setup(x => x.GetByIdAsync(productId))
			.ReturnsAsync((Domain.Entities.Product?)null);

		// Act
		var result = await sut.Handle(command, CancellationToken.None);

		// Assert
		result.IsSuccess.Should().BeFalse();
		result.Message.Should().Be("Product not found");
	}

	[Fact]
	public async Task Handle_WhenProductIsInactive_ReturnsError()
	{
		// Arrange
		var sut = CreateSut();
		var identityUserId = Guid.NewGuid();
		var product = new Domain.Entities.Product("Test Product");
		product.Deactivate();
		var command = new AddToCartCommand(identityUserId, product.Id, Guid.NewGuid(), 1);

		_productRepository.Setup(x => x.GetByIdAsync(product.Id))
			.ReturnsAsync(product);

		// Act
		var result = await sut.Handle(command, CancellationToken.None);

		// Assert
		result.IsSuccess.Should().BeFalse();
		result.Message.Should().Be("Product is not available");
	}

	[Fact]
	public async Task Handle_WhenSkuNotFound_ReturnsError()
	{
		// Arrange
		var sut = CreateSut();
		var identityUserId = Guid.NewGuid();
		var product = new Domain.Entities.Product("Test Product");
		var skuId = Guid.NewGuid();
		var command = new AddToCartCommand(identityUserId, product.Id, skuId, 1);

		_productRepository.Setup(x => x.GetByIdAsync(product.Id))
			.ReturnsAsync(product);
		_skuRepository.Setup(x => x.GetByIdAsync(skuId))
			.ReturnsAsync((Domain.Entities.SkuEntity?)null);

		// Act
		var result = await sut.Handle(command, CancellationToken.None);

		// Assert
		result.IsSuccess.Should().BeFalse();
		result.Message.Should().Be("Product variant not found");
	}

	[Fact]
	public async Task Handle_WhenInsufficientStock_ReturnsError()
	{
		// Arrange
		var sut = CreateSut();
		var identityUserId = Guid.NewGuid();
		var product = new Domain.Entities.Product("Test Product");
		var sku = Domain.Entities.SkuEntity.Create(product.Id, 10.00m, 5);
		var command = new AddToCartCommand(identityUserId, product.Id, sku.Id, 10);

		_productRepository.Setup(x => x.GetByIdAsync(product.Id))
			.ReturnsAsync(product);
		_skuRepository.Setup(x => x.GetByIdAsync(sku.Id))
			.ReturnsAsync(sku);

		// Act
		var result = await sut.Handle(command, CancellationToken.None);

		// Assert
		result.IsSuccess.Should().BeFalse();
		result.Message.Should().Contain("Insufficient stock");
	}

	[Fact]
	public async Task Handle_WhenCartDoesNotExist_CreatesNewCart()
	{
		// Arrange
		var sut = CreateSut();
		var identityUserId = Guid.NewGuid();
		var domainUserId = Guid.NewGuid();
		var product = new Domain.Entities.Product("Test Product");
		var sku = Domain.Entities.SkuEntity.Create(product.Id, 10.00m, 100);
		var command = new AddToCartCommand(identityUserId, product.Id, sku.Id, 2);

		_productRepository.Setup(x => x.GetByIdAsync(product.Id))
			.ReturnsAsync(product);
		_skuRepository.Setup(x => x.GetByIdAsync(sku.Id))
			.ReturnsAsync(sku);

		SetupCartServiceForSuccess(identityUserId, domainUserId);

		// Act
		var result = await sut.Handle(command, CancellationToken.None);

		// Assert
		result.IsSuccess.Should().BeTrue();
		// Note: Update() is not called because EF Core automatically tracks changes to entities
		_unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
	}

	[Fact]
	public async Task Handle_WhenCartExists_AddsItemToExistingCart()
	{
		// Arrange
		var sut = CreateSut();
		var identityUserId = Guid.NewGuid();
		var domainUserId = Guid.NewGuid();
		var product = new Domain.Entities.Product("Test Product");
		var sku = Domain.Entities.SkuEntity.Create(product.Id, 10.00m, 100);
		var existingCart = new Domain.Entities.Cart(domainUserId);
		var command = new AddToCartCommand(identityUserId, product.Id, sku.Id, 2);

		_productRepository.Setup(x => x.GetByIdAsync(product.Id))
			.ReturnsAsync(product);
		_skuRepository.Setup(x => x.GetByIdAsync(sku.Id))
			.ReturnsAsync(sku);

		SetupCartServiceForSuccess(identityUserId, domainUserId, existingCart);

		// Act
		var result = await sut.Handle(command, CancellationToken.None);

		// Assert
		result.IsSuccess.Should().BeTrue();
		// Note: Update() is not called because EF Core automatically tracks changes to entities
		_unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
	}

	[Fact]
	public async Task Handle_WhenItemAlreadyInCart_UpdatesQuantity()
	{
		// Arrange
		var sut = CreateSut();
		var identityUserId = Guid.NewGuid();
		var domainUserId = Guid.NewGuid();
		var product = new Domain.Entities.Product("Test Product");
		var sku = Domain.Entities.SkuEntity.Create(product.Id, 10.00m, 100);
		var existingCart = new Domain.Entities.Cart(domainUserId);
		existingCart.AddItem(product.Id, sku.Id, 2);
		var command = new AddToCartCommand(identityUserId, product.Id, sku.Id, 3);

		_productRepository.Setup(x => x.GetByIdAsync(product.Id))
			.ReturnsAsync(product);
		_skuRepository.Setup(x => x.GetByIdAsync(sku.Id))
			.ReturnsAsync(sku);

		SetupCartServiceForSuccess(identityUserId, domainUserId, existingCart);

		// Act
		var result = await sut.Handle(command, CancellationToken.None);

		// Assert
		result.IsSuccess.Should().BeTrue();
		existingCart.GetTotalItems().Should().Be(5);
	}

	[Fact]
	public async Task Handle_WhenExceedsMaxQuantity_ReturnsError()
	{
		// Arrange
		var sut = CreateSut();
		var identityUserId = Guid.NewGuid();
		var domainUserId = Guid.NewGuid();
		var domainUser = CreateDomainUser(identityUserId, domainUserId);
		var product = new Domain.Entities.Product("Test Product");
		var sku = Domain.Entities.SkuEntity.Create(product.Id, 10.00m, 200);
		var existingCart = new Domain.Entities.Cart(domainUserId);
		existingCart.AddItem(product.Id, sku.Id, 50);
		var command = new AddToCartCommand(identityUserId, product.Id, sku.Id, 60);

		_productRepository.Setup(x => x.GetByIdAsync(product.Id))
			.ReturnsAsync(product);
		_skuRepository.Setup(x => x.GetByIdAsync(sku.Id))
			.ReturnsAsync(sku);

		// Setup cart service to return failure for max quantity exceeded
		_cartService.Setup(x => x.GetOrCreateCartAsync(identityUserId, It.IsAny<CancellationToken>()))
			.ReturnsAsync(CartOperationResult<(Domain.Entities.Cart, Domain.Entities.User, bool)>.Success((existingCart, domainUser, false)));

		_cartService.Setup(x => x.ValidateTotalQuantity(60, 50))
			.Returns(CartOperationResult<int>.Failure("Maximum 99 items allowed per product variant"));

		var result = await sut.Handle(command, CancellationToken.None);

		// Assert
		result.IsSuccess.Should().BeFalse();
		result.Message.Should().Contain("Maximum");
	}
}
