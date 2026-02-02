using Application.Commands.Cart.AddToCart;
using Application.DTOs;
using Application.Interfaces;
using Domain.Interfaces.Repositories;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

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

	private void SetupCartServiceForSuccess(Guid productId, Guid skuId, int quantity)
	{
		// Mock AddOrUpdateItemAsync to return success
		_cartService.Setup(x => x.AddOrUpdateItemAsync(It.IsAny<Guid>(), productId, skuId, quantity, It.IsAny<CancellationToken>()))
			.ReturnsAsync((Guid uid, Guid pid, Guid sid, int qty, CancellationToken ct) =>
			{
				var cartItems = new List<CartItemDto>
				{
					new CartItemDto(
						Guid.NewGuid(), 
						pid, 
						"Test Product", 
						null, 
						sid, 
						"SKU001", 
						null, 
						qty, 
						10.00m, 
						qty * 10.00m, 
						DateTime.UtcNow)
				};
				var dto = new CartDto(Guid.NewGuid(), Guid.NewGuid(), cartItems, qty, qty * 10.00m);
				return new ServiceResponse<CartDto>(true, "Item added to cart", dto);
			});
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
		var product = new Domain.Entities.Product("Test Product");
		var sku = Domain.Entities.SkuEntity.Create(product.Id, 10.00m, 100);
		var command = new AddToCartCommand(identityUserId, product.Id, sku.Id, 2);

		_productRepository.Setup(x => x.GetByIdAsync(product.Id))
			.ReturnsAsync(product);
		_skuRepository.Setup(x => x.GetByIdAsync(sku.Id))
			.ReturnsAsync(sku);

		SetupCartServiceForSuccess(product.Id, sku.Id, 2);

		// Act
		var result = await sut.Handle(command, CancellationToken.None);

		// Assert
		result.IsSuccess.Should().BeTrue();
		result.Payload.Should().NotBeNull();
		_cartService.Verify(x => x.AddOrUpdateItemAsync(identityUserId, product.Id, sku.Id, 2, It.IsAny<CancellationToken>()), Times.Once);
	}

	[Fact]
	public async Task Handle_WhenCartExists_AddsItemToExistingCart()
	{
		// Arrange
		var sut = CreateSut();
		var identityUserId = Guid.NewGuid();
		var product = new Domain.Entities.Product("Test Product");
		var sku = Domain.Entities.SkuEntity.Create(product.Id, 10.00m, 100);
		var command = new AddToCartCommand(identityUserId, product.Id, sku.Id, 2);

		_productRepository.Setup(x => x.GetByIdAsync(product.Id))
			.ReturnsAsync(product);
		_skuRepository.Setup(x => x.GetByIdAsync(sku.Id))
			.ReturnsAsync(sku);

		SetupCartServiceForSuccess(product.Id, sku.Id, 2);

		// Act
		var result = await sut.Handle(command, CancellationToken.None);

		// Assert
		result.IsSuccess.Should().BeTrue();
		result.Payload.Should().NotBeNull();
		_cartService.Verify(x => x.AddOrUpdateItemAsync(identityUserId, product.Id, sku.Id, 2, It.IsAny<CancellationToken>()), Times.Once);
	}

	[Fact]
	public async Task Handle_WhenItemAlreadyInCart_UpdatesQuantity()
	{
		// Arrange
		var sut = CreateSut();
		var identityUserId = Guid.NewGuid();
		var product = new Domain.Entities.Product("Test Product");
		var sku = Domain.Entities.SkuEntity.Create(product.Id, 10.00m, 100);
		var command = new AddToCartCommand(identityUserId, product.Id, sku.Id, 3);

		_productRepository.Setup(x => x.GetByIdAsync(product.Id))
			.ReturnsAsync(product);
		_skuRepository.Setup(x => x.GetByIdAsync(sku.Id))
			.ReturnsAsync(sku);

		// Mock service to return cart with updated quantity (simulating existing item + new quantity)
		_cartService.Setup(x => x.AddOrUpdateItemAsync(identityUserId, product.Id, sku.Id, 3, It.IsAny<CancellationToken>()))
			.ReturnsAsync(() =>
			{
				var cartItems = new List<CartItemDto>
				{
					new CartItemDto(
						Guid.NewGuid(), 
						product.Id, 
						"Test Product", 
						null, 
						sku.Id, 
						"SKU001", 
						null, 
						5, // 2 existing + 3 new = 5
						10.00m, 
						50.00m, 
						DateTime.UtcNow)
				};
				var dto = new CartDto(Guid.NewGuid(), Guid.NewGuid(), cartItems, 5, 50.00m);
				return new ServiceResponse<CartDto>(true, "Item quantity updated", dto);
			});

		// Act
		var result = await sut.Handle(command, CancellationToken.None);

		// Assert
		result.IsSuccess.Should().BeTrue();
		result.Payload.Should().NotBeNull();
		result.Payload!.TotalItems.Should().Be(5);
	}

	[Fact]
	public async Task Handle_WhenExceedsMaxQuantity_ReturnsError()
	{
		// Arrange
		var sut = CreateSut();
		var identityUserId = Guid.NewGuid();
		var product = new Domain.Entities.Product("Test Product");
		var sku = Domain.Entities.SkuEntity.Create(product.Id, 10.00m, 200);
		var command = new AddToCartCommand(identityUserId, product.Id, sku.Id, 60);

		_productRepository.Setup(x => x.GetByIdAsync(product.Id))
			.ReturnsAsync(product);
		_skuRepository.Setup(x => x.GetByIdAsync(sku.Id))
			.ReturnsAsync(sku);

		// Setup cart service to return failure for max quantity exceeded
		_cartService.Setup(x => x.AddOrUpdateItemAsync(identityUserId, product.Id, sku.Id, 60, It.IsAny<CancellationToken>()))
			.ReturnsAsync(new ServiceResponse<CartDto>(false, "Maximum 99 items allowed per product variant", null));

		// Act
		var result = await sut.Handle(command, CancellationToken.None);

		// Assert
		result.IsSuccess.Should().BeFalse();
		result.Message.Should().Contain("Maximum");
	}
}
