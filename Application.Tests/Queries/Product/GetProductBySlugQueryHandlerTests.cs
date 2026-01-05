using Application.DTOs;
using Application.Interfaces;
using Application.Queries.Catalog.GetProductBySlug;
using Domain.Entities;
using Domain.Interfaces.Repositories;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace Application.Tests.Queries.Product;

public class GetProductBySlugQueryHandlerTests
{
	private readonly Mock<IProductRepository> _productRepository = new();
	private readonly Mock<IFileStorage> _fileStorage = new();
	private readonly Mock<ILogger<GetProductBySlugQueryHandler>> _logger = new();

	private GetProductBySlugQueryHandler CreateSut()
		=> new(_productRepository.Object, _fileStorage.Object, _logger.Object);

	private static Domain.Entities.Product CreateProductWithSkus(string name, int skuCount = 2)
	{
		var product = new Domain.Entities.Product(name, "Test description");

		for (int i = 0; i < skuCount; i++)
		{
			var sku = SkuEntity.Create(product.Id, 100m + i * 10, 10 + i);
			product.AddSku(sku);
		}

		return product;
	}

	[Fact]
	public async Task Handle_WhenProductExists_ReturnsSuccessWithProduct()
	{
		// Arrange
		var product = CreateProductWithSkus("Test Product");
		_productRepository.Setup(x => x.GetBySlugAsync(product.Slug)).ReturnsAsync(product);
		_fileStorage.Setup(x => x.GetPublicUrl(It.IsAny<string>())).Returns("http://example.com/image.jpg");

		var sut = CreateSut();
		var query = new GetProductBySlugQuery(product.Slug);

		// Act
		var result = await sut.Handle(query, CancellationToken.None);

		// Assert
		result.IsSuccess.Should().BeTrue();
		result.Payload.Should().NotBeNull();
		result.Payload!.Name.Should().Be("Test Product");
		result.Payload.Slug.Should().Be(product.Slug);
		result.Payload.Skus.Should().HaveCount(2);
	}

	[Fact]
	public async Task Handle_WhenProductNotFound_ReturnsFailure()
	{
		// Arrange
		_productRepository.Setup(x => x.GetBySlugAsync(It.IsAny<string>())).ReturnsAsync((Domain.Entities.Product?)null);

		var sut = CreateSut();
		var query = new GetProductBySlugQuery("non-existent-slug");

		// Act
		var result = await sut.Handle(query, CancellationToken.None);

		// Assert
		result.IsSuccess.Should().BeFalse();
		result.Message.Should().Contain("not found");
		result.Payload.Should().BeNull();
	}

	[Fact]
	public async Task Handle_WhenSkuCodeProvided_ReordersSkusToMatchFirst()
	{
		// Arrange
		var product = CreateProductWithSkus("Test Product", 3);
		var targetSku = product.Skus.Last();

		_productRepository.Setup(x => x.GetBySlugAsync(product.Slug)).ReturnsAsync(product);
		_fileStorage.Setup(x => x.GetPublicUrl(It.IsAny<string>())).Returns("http://example.com/image.jpg");

		var sut = CreateSut();
		var query = new GetProductBySlugQuery(product.Slug, targetSku.SkuCode);

		// Act
		var result = await sut.Handle(query, CancellationToken.None);

		// Assert
		result.IsSuccess.Should().BeTrue();
		result.Payload.Should().NotBeNull();
		result.Payload!.Skus.Should().HaveCount(3);
		result.Payload.Skus.First().SkuCode.Should().Be(targetSku.SkuCode);
	}

	[Fact]
	public async Task Handle_WhenSkuCodeNotFound_ReturnsProductWithOriginalOrder()
	{
		// Arrange
		var product = CreateProductWithSkus("Test Product", 2);
		var firstSkuCode = product.Skus.First().SkuCode;

		_productRepository.Setup(x => x.GetBySlugAsync(product.Slug)).ReturnsAsync(product);
		_fileStorage.Setup(x => x.GetPublicUrl(It.IsAny<string>())).Returns("http://example.com/image.jpg");

		var sut = CreateSut();
		var query = new GetProductBySlugQuery(product.Slug, "non-existent-sku-code");

		// Act
		var result = await sut.Handle(query, CancellationToken.None);

		// Assert
		result.IsSuccess.Should().BeTrue();
		result.Payload.Should().NotBeNull();
		result.Payload!.Skus.First().SkuCode.Should().Be(firstSkuCode);
	}

	[Fact]
	public async Task Handle_WhenSlugIsCaseInsensitive_FindsProduct()
	{
		// Arrange
		var product = CreateProductWithSkus("Test Product");
		var upperSlug = product.Slug.ToUpperInvariant();

		_productRepository.Setup(x => x.GetBySlugAsync(upperSlug)).ReturnsAsync(product);
		_fileStorage.Setup(x => x.GetPublicUrl(It.IsAny<string>())).Returns("http://example.com/image.jpg");

		var sut = CreateSut();
		var query = new GetProductBySlugQuery(upperSlug);

		// Act
		var result = await sut.Handle(query, CancellationToken.None);

		// Assert
		result.IsSuccess.Should().BeTrue();
		result.Payload.Should().NotBeNull();
	}

	[Fact]
	public async Task Handle_WhenRepositoryThrowsException_ReturnsFailure()
	{
		// Arrange
		_productRepository.Setup(x => x.GetBySlugAsync(It.IsAny<string>())).ThrowsAsync(new Exception("Database error"));

		var sut = CreateSut();
		var query = new GetProductBySlugQuery("some-slug");

		// Act
		var result = await sut.Handle(query, CancellationToken.None);

		// Assert
		result.IsSuccess.Should().BeFalse();
		result.Message.Should().Contain("Database error");
	}

	[Fact]
	public async Task Handle_WhenProductHasNoSkus_ReturnsProductWithEmptySkusList()
	{
		// Arrange
		var product = new Domain.Entities.Product("No SKUs Product", "desc");

		_productRepository.Setup(x => x.GetBySlugAsync(product.Slug)).ReturnsAsync(product);
		_fileStorage.Setup(x => x.GetPublicUrl(It.IsAny<string>())).Returns("http://example.com/image.jpg");

		var sut = CreateSut();
		var query = new GetProductBySlugQuery(product.Slug);

		// Act
		var result = await sut.Handle(query, CancellationToken.None);

		// Assert
		result.IsSuccess.Should().BeTrue();
		result.Payload.Should().NotBeNull();
		result.Payload!.Skus.Should().BeEmpty();
	}
}
