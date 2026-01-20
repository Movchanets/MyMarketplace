using Application.DTOs;
using Application.Queries.Catalog.FilterProducts;
using Domain.Entities;
using Domain.Interfaces.Repositories;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace Application.Tests.Queries.Catalog;

public class FilterProductsQueryHandlerTests
{
	private readonly Mock<IProductRepository> _productRepository = new();
	private readonly Mock<ILogger<FilterProductsQueryHandler>> _logger = new();

	private FilterProductsQueryHandler CreateSut()
		=> new(_productRepository.Object, _logger.Object);

	private static Domain.Entities.Product CreateProductWithSkus(string name, decimal price, int stock)
	{
		var product = new Domain.Entities.Product(name, "Test description");
		var sku = SkuEntity.Create(product.Id, price, stock);
		product.AddSku(sku);
		return product;
	}

	[Fact]
	public async Task Handle_WhenValidQuery_ReturnsPagedProducts()
	{
		// Arrange
		var products = new List<Domain.Entities.Product>
		{
			CreateProductWithSkus("Product 1", 100m, 10),
			CreateProductWithSkus("Product 2", 200m, 5)
		};

		_productRepository.Setup(x => x.FilterAsync(
			It.IsAny<Guid?>(),
			It.IsAny<List<Guid>?>(),
			It.IsAny<decimal?>(),
			It.IsAny<decimal?>(),
			It.IsAny<bool?>(),
			It.IsAny<Dictionary<string, object>?>(),
			It.IsAny<string>(),
			It.IsAny<int>(),
			It.IsAny<int>()
		)).ReturnsAsync((products, 2));

		var sut = CreateSut();
		var query = new FilterProductsQuery(
			CategoryId: Guid.NewGuid(),
			MinPrice: 50m,
			MaxPrice: 300m,
			Page: 1,
			PageSize: 24
		);

		// Act
		var result = await sut.Handle(query, CancellationToken.None);

		// Assert
		result.IsSuccess.Should().BeTrue();
		result.Payload.Should().NotBeNull();
		result.Payload!.Items.Should().HaveCount(2);
		result.Payload.Total.Should().Be(2);
		result.Payload.Page.Should().Be(1);
		result.Payload.PageSize.Should().Be(24);
	}

	[Fact]
	public async Task Handle_WhenNoProductsFound_ReturnsEmptyResults()
	{
		// Arrange
		_productRepository.Setup(x => x.FilterAsync(
			It.IsAny<Guid?>(),
			It.IsAny<List<Guid>?>(),
			It.IsAny<decimal?>(),
			It.IsAny<decimal?>(),
			It.IsAny<bool?>(),
			It.IsAny<Dictionary<string, object>?>(),
			It.IsAny<string>(),
			It.IsAny<int>(),
			It.IsAny<int>()
		)).ReturnsAsync((new List<Domain.Entities.Product>(), 0));

		var sut = CreateSut();
		var query = new FilterProductsQuery(CategoryId: Guid.NewGuid());

		// Act
		var result = await sut.Handle(query, CancellationToken.None);

		// Assert
		result.IsSuccess.Should().BeTrue();
		result.Payload.Should().NotBeNull();
		result.Payload!.Items.Should().BeEmpty();
		result.Payload.Total.Should().Be(0);
	}

	[Fact]
	public async Task Handle_WhenInvalidPage_ReturnsFailure()
	{
		// Arrange
		var sut = CreateSut();
		var query = new FilterProductsQuery(Page: 0);

		// Act
		var result = await sut.Handle(query, CancellationToken.None);

		// Assert
		result.IsSuccess.Should().BeFalse();
		result.Message.Should().Contain("Page must be >= 1");
	}

	[Fact]
	public async Task Handle_WhenInvalidPageSize_ReturnsFailure()
	{
		// Arrange
		var sut = CreateSut();
		var query = new FilterProductsQuery(PageSize: 0);

		// Act
		var result = await sut.Handle(query, CancellationToken.None);

		// Assert
		result.IsSuccess.Should().BeFalse();
		result.Message.Should().Contain("PageSize must be between 1 and 100");
	}

	[Fact]
	public async Task Handle_WhenPageSizeExceedsMax_ReturnsFailure()
	{
		// Arrange
		var sut = CreateSut();
		var query = new FilterProductsQuery(PageSize: 150);

		// Act
		var result = await sut.Handle(query, CancellationToken.None);

		// Assert
		result.IsSuccess.Should().BeFalse();
		result.Message.Should().Contain("PageSize must be between 1 and 100");
	}

	[Fact]
	public async Task Handle_WhenAttributeFiltersProvided_PassesToRepository()
	{
		// Arrange
		var products = new List<Domain.Entities.Product> { CreateProductWithSkus("Product", 100m, 10) };
		
		Dictionary<string, object>? capturedFilters = null;
		_productRepository.Setup(x => x.FilterAsync(
			It.IsAny<Guid?>(),
			It.IsAny<List<Guid>?>(),
			It.IsAny<decimal?>(),
			It.IsAny<decimal?>(),
			It.IsAny<bool?>(),
			It.IsAny<Dictionary<string, object>?>(),
			It.IsAny<string>(),
			It.IsAny<int>(),
			It.IsAny<int>()
		))
		.Callback<Guid?, List<Guid>?, decimal?, decimal?, bool?, Dictionary<string, object>?, string, int, int>(
			(_, _, _, _, _, filters, _, _, _) => capturedFilters = filters
		)
		.ReturnsAsync((products, 1));

		var sut = CreateSut();
		var query = new FilterProductsQuery(
			Attributes: new Dictionary<string, AttributeFilterValue>
			{
				["color"] = new AttributeFilterValue { In = new List<string> { "Black", "Blue" } },
				["storage"] = new AttributeFilterValue { Gte = 128m, Lte = 512m }
			}
		);

		// Act
		var result = await sut.Handle(query, CancellationToken.None);

		// Assert
		result.IsSuccess.Should().BeTrue();
		capturedFilters.Should().NotBeNull();
		capturedFilters.Should().ContainKey("color");
		capturedFilters.Should().ContainKey("storage");
	}

	[Fact]
	public async Task Handle_WhenRepositoryThrowsException_ReturnsFailure()
	{
		// Arrange
		_productRepository.Setup(x => x.FilterAsync(
			It.IsAny<Guid?>(),
			It.IsAny<List<Guid>?>(),
			It.IsAny<decimal?>(),
			It.IsAny<decimal?>(),
			It.IsAny<bool?>(),
			It.IsAny<Dictionary<string, object>?>(),
			It.IsAny<string>(),
			It.IsAny<int>(),
			It.IsAny<int>()
		)).ThrowsAsync(new Exception("Database error"));

		var sut = CreateSut();
		var query = new FilterProductsQuery();

		// Act
		var result = await sut.Handle(query, CancellationToken.None);

		// Assert
		result.IsSuccess.Should().BeFalse();
		result.Message.Should().Contain("Database error");
	}

	[Fact]
	public async Task Handle_WhenSortingSpecified_PassesCorrectSortToRepository()
	{
		// Arrange
		var products = new List<Domain.Entities.Product> { CreateProductWithSkus("Product", 100m, 10) };
		
		string? capturedSort = null;
		_productRepository.Setup(x => x.FilterAsync(
			It.IsAny<Guid?>(),
			It.IsAny<List<Guid>?>(),
			It.IsAny<decimal?>(),
			It.IsAny<decimal?>(),
			It.IsAny<bool?>(),
			It.IsAny<Dictionary<string, object>?>(),
			It.IsAny<string>(),
			It.IsAny<int>(),
			It.IsAny<int>()
		))
		.Callback<Guid?, List<Guid>?, decimal?, decimal?, bool?, Dictionary<string, object>?, string, int, int>(
			(_, _, _, _, _, _, sort, _, _) => capturedSort = sort
		)
		.ReturnsAsync((products, 1));

		var sut = CreateSut();
		var query = new FilterProductsQuery(Sort: ProductSort.PriceAsc);

		// Act
		var result = await sut.Handle(query, CancellationToken.None);

		// Assert
		result.IsSuccess.Should().BeTrue();
		capturedSort.Should().Be("PriceAsc");
	}
}
