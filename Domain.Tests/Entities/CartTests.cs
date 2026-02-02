using Domain.Entities;
using FluentAssertions;
using Xunit;

namespace Domain.Tests.Entities;

public class CartTests
{
	[Fact]
	public void Constructor_WithValidUserId_CreatesCart()
	{
		// Arrange
		var userId = Guid.NewGuid();

		// Act
		var cart = new Cart(userId);

		// Assert
		cart.UserId.Should().Be(userId);
		cart.Items.Should().BeEmpty();
		cart.GetTotalItems().Should().Be(0);
	}

	[Fact]
	public void Constructor_WithEmptyUserId_ThrowsArgumentException()
	{
		// Arrange
		var userId = Guid.Empty;

		// Act
		Action act = () => new Cart(userId);

		// Assert
		act.Should().Throw<ArgumentException>().WithMessage("*UserId cannot be empty*");
	}

	[Fact]
	public void AddItem_WithValidData_AddsItemToCart()
	{
		// Arrange
		var cart = new Cart(Guid.NewGuid());
		var productId = Guid.NewGuid();
		var skuId = Guid.NewGuid();
		var quantity = 2;

		// Act
		cart.AddItem(productId, skuId, quantity);

		// Assert
		cart.Items.Should().HaveCount(1);
		cart.GetTotalItems().Should().Be(2);
		cart.ContainsSku(skuId).Should().BeTrue();
	}

	[Fact]
	public void AddItem_WithExistingSku_UpdatesQuantity()
	{
		// Arrange
		var cart = new Cart(Guid.NewGuid());
		var productId = Guid.NewGuid();
		var skuId = Guid.NewGuid();
		cart.AddItem(productId, skuId, 2);

		// Act
		cart.AddItem(productId, skuId, 3);

		// Assert
		cart.Items.Should().HaveCount(1);
		cart.GetTotalItems().Should().Be(5);
		cart.GetSkuQuantity(skuId).Should().Be(5);
	}

	[Fact]
	public void AddItem_WithZeroQuantity_ThrowsArgumentException()
	{
		// Arrange
		var cart = new Cart(Guid.NewGuid());

		// Act
		Action act = () => cart.AddItem(Guid.NewGuid(), Guid.NewGuid(), 0);

		// Assert
		act.Should().Throw<ArgumentException>().WithMessage("*Quantity must be greater than zero*");
	}

	[Fact]
	public void AddItem_WithNegativeQuantity_ThrowsArgumentException()
	{
		// Arrange
		var cart = new Cart(Guid.NewGuid());

		// Act
		Action act = () => cart.AddItem(Guid.NewGuid(), Guid.NewGuid(), -1);

		// Assert
		act.Should().Throw<ArgumentException>().WithMessage("*Quantity must be greater than zero*");
	}

	[Fact]
	public void UpdateItemQuantity_WithValidQuantity_UpdatesItem()
	{
		// Arrange
		var cart = new Cart(Guid.NewGuid());
		var productId = Guid.NewGuid();
		var skuId = Guid.NewGuid();
		cart.AddItem(productId, skuId, 2);
		var cartItemId = cart.Items.First().Id;

		// Act
		cart.UpdateItemQuantity(cartItemId, 5);

		// Assert
		cart.GetTotalItems().Should().Be(5);
	}

	[Fact]
	public void UpdateItemQuantity_ToZero_RemovesItem()
	{
		// Arrange
		var cart = new Cart(Guid.NewGuid());
		var productId = Guid.NewGuid();
		var skuId = Guid.NewGuid();
		cart.AddItem(productId, skuId, 2);
		var cartItemId = cart.Items.First().Id;

		// Act
		cart.UpdateItemQuantity(cartItemId, 0);

		// Assert
		cart.Items.Should().BeEmpty();
		cart.ContainsSku(skuId).Should().BeFalse();
	}

	[Fact]
	public void UpdateItemQuantity_WithNegativeQuantity_ThrowsArgumentException()
	{
		// Arrange
		var cart = new Cart(Guid.NewGuid());
		var productId = Guid.NewGuid();
		var skuId = Guid.NewGuid();
		cart.AddItem(productId, skuId, 2);
		var cartItemId = cart.Items.First().Id;

		// Act
		Action act = () => cart.UpdateItemQuantity(cartItemId, -1);

		// Assert
		act.Should().Throw<ArgumentException>().WithMessage("*Quantity cannot be negative*");
	}

	[Fact]
	public void UpdateItemQuantity_WithNonExistentItem_ThrowsInvalidOperationException()
	{
		// Arrange
		var cart = new Cart(Guid.NewGuid());

		// Act
		Action act = () => cart.UpdateItemQuantity(Guid.NewGuid(), 5);

		// Assert
		act.Should().Throw<InvalidOperationException>().WithMessage("*not found*");
	}

	[Fact]
	public void RemoveItem_WithExistingItem_RemovesItem()
	{
		// Arrange
		var cart = new Cart(Guid.NewGuid());
		var productId = Guid.NewGuid();
		var skuId = Guid.NewGuid();
		cart.AddItem(productId, skuId, 2);
		var cartItemId = cart.Items.First().Id;

		// Act
		cart.RemoveItem(cartItemId);

		// Assert
		cart.Items.Should().BeEmpty();
		cart.ContainsSku(skuId).Should().BeFalse();
	}

	[Fact]
	public void RemoveItem_WithNonExistentItem_DoesNothing()
	{
		// Arrange
		var cart = new Cart(Guid.NewGuid());
		cart.AddItem(Guid.NewGuid(), Guid.NewGuid(), 2);

		// Act
		cart.RemoveItem(Guid.NewGuid());

		// Assert
		cart.Items.Should().HaveCount(1);
	}

	[Fact]
	public void RemoveItemBySku_WithExistingSku_RemovesItem()
	{
		// Arrange
		var cart = new Cart(Guid.NewGuid());
		var productId = Guid.NewGuid();
		var skuId = Guid.NewGuid();
		cart.AddItem(productId, skuId, 2);

		// Act
		cart.RemoveItemBySku(skuId);

		// Assert
		cart.Items.Should().BeEmpty();
	}

	[Fact]
	public void Clear_WithItems_RemovesAllItems()
	{
		// Arrange
		var cart = new Cart(Guid.NewGuid());
		cart.AddItem(Guid.NewGuid(), Guid.NewGuid(), 2);
		cart.AddItem(Guid.NewGuid(), Guid.NewGuid(), 3);

		// Act
		cart.Clear();

		// Assert
		cart.Items.Should().BeEmpty();
		cart.GetTotalItems().Should().Be(0);
	}

	[Fact]
	public void Clear_WithEmptyCart_DoesNothing()
	{
		// Arrange
		var cart = new Cart(Guid.NewGuid());

		// Act
		cart.Clear();

		// Assert
		cart.Items.Should().BeEmpty();
	}

	[Fact]
	public void ContainsSku_WithExistingSku_ReturnsTrue()
	{
		// Arrange
		var cart = new Cart(Guid.NewGuid());
		var skuId = Guid.NewGuid();
		cart.AddItem(Guid.NewGuid(), skuId, 1);

		// Act & Assert
		cart.ContainsSku(skuId).Should().BeTrue();
	}

	[Fact]
	public void ContainsSku_WithNonExistentSku_ReturnsFalse()
	{
		// Arrange
		var cart = new Cart(Guid.NewGuid());

		// Act & Assert
		cart.ContainsSku(Guid.NewGuid()).Should().BeFalse();
	}

	[Fact]
	public void GetSkuQuantity_WithExistingSku_ReturnsQuantity()
	{
		// Arrange
		var cart = new Cart(Guid.NewGuid());
		var skuId = Guid.NewGuid();
		cart.AddItem(Guid.NewGuid(), skuId, 5);

		// Act & Assert
		cart.GetSkuQuantity(skuId).Should().Be(5);
	}

	[Fact]
	public void GetSkuQuantity_WithNonExistentSku_ReturnsZero()
	{
		// Arrange
		var cart = new Cart(Guid.NewGuid());

		// Act & Assert
		cart.GetSkuQuantity(Guid.NewGuid()).Should().Be(0);
	}

	[Fact]
	public void GetTotalItems_WithMultipleItems_ReturnsSum()
	{
		// Arrange
		var cart = new Cart(Guid.NewGuid());
		cart.AddItem(Guid.NewGuid(), Guid.NewGuid(), 2);
		cart.AddItem(Guid.NewGuid(), Guid.NewGuid(), 3);
		cart.AddItem(Guid.NewGuid(), Guid.NewGuid(), 5);

		// Act & Assert
		cart.GetTotalItems().Should().Be(10);
	}
}
