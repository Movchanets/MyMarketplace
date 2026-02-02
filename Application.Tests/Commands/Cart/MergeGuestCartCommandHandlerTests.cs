using Application.Commands.Cart.MergeGuestCart;
using Application.Interfaces;
using Domain.Entities;
using Application.DTOs;
using Application.Commands.Cart.AddToCart;
using Domain.Interfaces.Repositories;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Application.Tests.Commands.Cart;

public class MergeGuestCartCommandHandlerTests
{
	private readonly Mock<ICartService> _cartService = new();
	private readonly Mock<ICartRepository> _cartRepository = new();
	private readonly Mock<IProductRepository> _productRepository = new();
	private readonly Mock<ISkuRepository> _skuRepository = new();
	private readonly Mock<IUnitOfWork> _unitOfWork = new();
	private readonly Mock<ILogger<MergeGuestCartCommandHandler>> _logger = new();

	private MergeGuestCartCommandHandler CreateSut()
		=> new(
			_cartService.Object,
			_cartRepository.Object,
			_productRepository.Object,
			_skuRepository.Object,
			_unitOfWork.Object,
			_logger.Object);

	[Fact]
	public async Task Handle_WhenNoItems_ReturnsNoItemsToMerge()
	{
		// Arrange
		var sut = CreateSut();
		var cmd = new MergeGuestCartCommand(Guid.NewGuid(), new List<MergeCartItemDto>());

		// Act
		var result = await sut.Handle(cmd, CancellationToken.None);

		// Assert
		result.IsSuccess.Should().BeTrue();
		result.Message.Should().Be("No items to merge");
	}

	[Fact]
	public async Task Handle_WhenAllItemsInvalid_ReturnsNoValidItems()
	{
		// Arrange
		var sut = CreateSut();
		var userId = Guid.NewGuid();
		var item = new MergeCartItemDto(Guid.NewGuid(), Guid.NewGuid(), 2);
		var cmd = new MergeGuestCartCommand(userId, new List<MergeCartItemDto> { item });

		// Product repo returns null (invalid product)
		_productRepository.Setup(x => x.GetByIdAsync(item.ProductId)).ReturnsAsync((Domain.Entities.Product?)null);

		// Act
		var result = await sut.Handle(cmd, CancellationToken.None);

		// Assert
		result.IsSuccess.Should().BeFalse();
		result.Message.Should().Be("No valid items to add to cart");
	}

	[Fact]
	public async Task Handle_WhenValidItems_MergesIntoCart_Success()
	{
		// Arrange
		var sut = CreateSut();
		var identityUserId = Guid.NewGuid();
		var domainUserId = Guid.NewGuid();

		var product = new Domain.Entities.Product("Test Product");
		var sku = SkuEntity.Create(product.Id, 12.5m, 10);

		var cmd = new MergeGuestCartCommand(identityUserId,
			new List<MergeCartItemDto> { new MergeCartItemDto(product.Id, sku.Id, 2) });

		// Product and SKU exist and valid
		_productRepository.Setup(x => x.GetByIdAsync(product.Id)).ReturnsAsync((Domain.Entities.Product)product);
		_skuRepository.Setup(x => x.GetByIdAsync(sku.Id)).ReturnsAsync(sku);

		// CartService returns an existing cart
		var cart = new Domain.Entities.Cart(domainUserId);
		_cartService.Setup(x => x.GetOrCreateCartAsync(identityUserId, It.IsAny<CancellationToken>()))
			.ReturnsAsync(CartOperationResult<(Domain.Entities.Cart, Domain.Entities.User, bool)>.Success((cart, CreateDomainUser(identityUserId, domainUserId), false)));

		// MapToCartDto
		_cartService.Setup(x => x.MapToCartDto(It.IsAny<Domain.Entities.Cart>()))
			.Returns((Domain.Entities.Cart c) => new CartDto(c.Id, c.UserId, new List<CartItemDto>(), c.GetTotalItems(), 0m));

		// Act
		var result = await sut.Handle(cmd, CancellationToken.None);

		// Assert
		result.IsSuccess.Should().BeTrue();
		result.Message.Should().Contain("Added");
		_unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
	}

	private static Domain.Entities.User CreateDomainUser(Guid identityUserId, Guid domainUserId)
	{
		var user = new Domain.Entities.User(identityUserId, email: $"user_{identityUserId:N}@example.com");
		typeof(Domain.Entities.BaseEntity<Guid>)
			.GetProperty(nameof(Domain.Entities.BaseEntity<Guid>.Id))
			?.SetValue(user, domainUserId);
		return user;
	}
}
