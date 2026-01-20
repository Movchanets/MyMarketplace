using Application.Queries.Catalog.GetCategoryAvailableFilters;
using Domain.Entities;
using Domain.Interfaces.Repositories;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace Application.Tests.Queries.Catalog;

public class GetCategoryAvailableFiltersQueryHandlerTests
{
	private readonly Mock<IProductRepository> _productRepository = new();
	private readonly Mock<ICategoryRepository> _categoryRepository = new();
	private readonly Mock<IAttributeDefinitionRepository> _attributeDefinitionRepository = new();
	private readonly Mock<ILogger<GetCategoryAvailableFiltersQueryHandler>> _logger = new();

	private GetCategoryAvailableFiltersQueryHandler CreateSut()
		=> new(
			_productRepository.Object,
			_categoryRepository.Object,
			_attributeDefinitionRepository.Object,
			_logger.Object
		);

	private static Category CreateCategory(string name)
	{
		return Category.Create(name);
	}

	private static Domain.Entities.Product CreateProductWithSkuAttributes(
		string name,
		decimal price,
		int stock,
		Dictionary<string, object>? attributes = null)
	{
		var product = new Domain.Entities.Product(name, "Test description");
		var sku = SkuEntity.Create(product.Id, price, stock, attributes);
		product.AddSku(sku);
		return product;
	}

	private static AttributeDefinition CreateAttributeDefinition(
		string code,
		string name,
		string dataType = "string",
		int displayOrder = 0)
	{
		return new AttributeDefinition(code, name, dataType, displayOrder: displayOrder);
	}

	[Fact]
	public async Task Handle_WhenCategoryHasProducts_ReturnsAvailableFilters()
	{
		// Arrange
		var categoryId = Guid.NewGuid();
		var category = CreateCategory("Smartphones");
		
		var products = new List<Domain.Entities.Product>
		{
			CreateProductWithSkuAttributes("Product 1", 100m, 10, new Dictionary<string, object>
			{
				["color"] = "Black",
				["storage"] = 128
			}),
			CreateProductWithSkuAttributes("Product 2", 200m, 5, new Dictionary<string, object>
			{
				["color"] = "Blue",
				["storage"] = 256
			})
		};

		var attributeDefinitions = new List<AttributeDefinition>
		{
			CreateAttributeDefinition("color", "Color", "string", 1),
			CreateAttributeDefinition("storage", "Storage", "number", 2)
		};

		_categoryRepository.Setup(x => x.GetByIdAsync(categoryId)).ReturnsAsync(category);
		_productRepository.Setup(x => x.GetActiveByCategoryIdWithSkusAsync(categoryId))
			.ReturnsAsync(products);
		_attributeDefinitionRepository.Setup(x => x.GetByCodesAsync(It.IsAny<IEnumerable<string>>()))
			.ReturnsAsync(attributeDefinitions);

		var sut = CreateSut();
		var query = new GetCategoryAvailableFiltersQuery(categoryId);

		// Act
		var result = await sut.Handle(query, CancellationToken.None);

		// Assert
		result.IsSuccess.Should().BeTrue();
		result.Payload.Should().NotBeNull();
		result.Payload!.CategoryName.Should().Be("Smartphones");
		result.Payload.Attributes.Should().HaveCount(2);
		result.Payload.PriceRange.Should().NotBeNull();
		result.Payload.PriceRange!.Min.Should().Be(100m);
		result.Payload.PriceRange.Max.Should().Be(200m);
		result.Payload.TotalProductCount.Should().Be(2);
	}

	[Fact]
	public async Task Handle_WhenCategoryNotFound_ReturnsFailure()
	{
		// Arrange
		var categoryId = Guid.NewGuid();
		_categoryRepository.Setup(x => x.GetByIdAsync(categoryId)).ReturnsAsync((Category?)null);

		var sut = CreateSut();
		var query = new GetCategoryAvailableFiltersQuery(categoryId);

		// Act
		var result = await sut.Handle(query, CancellationToken.None);

		// Assert
		result.IsSuccess.Should().BeFalse();
		result.Message.Should().Contain("Category not found");
	}

	[Fact]
	public async Task Handle_WhenEmptyCategoryId_ReturnsFailure()
	{
		// Arrange
		var sut = CreateSut();
		var query = new GetCategoryAvailableFiltersQuery(Guid.Empty);

		// Act
		var result = await sut.Handle(query, CancellationToken.None);

		// Assert
		result.IsSuccess.Should().BeFalse();
		result.Message.Should().Contain("CategoryId is required");
	}

	[Fact]
	public async Task Handle_WhenCategoryHasNoProducts_ReturnsEmptyFilters()
	{
		// Arrange
		var categoryId = Guid.NewGuid();
		var category = CreateCategory("Empty Category");

		_categoryRepository.Setup(x => x.GetByIdAsync(categoryId)).ReturnsAsync(category);
		_productRepository.Setup(x => x.GetActiveByCategoryIdWithSkusAsync(categoryId))
			.ReturnsAsync(new List<Domain.Entities.Product>());

		var sut = CreateSut();
		var query = new GetCategoryAvailableFiltersQuery(categoryId);

		// Act
		var result = await sut.Handle(query, CancellationToken.None);

		// Assert
		result.IsSuccess.Should().BeTrue();
		result.Payload.Should().NotBeNull();
		result.Payload!.Attributes.Should().BeEmpty();
		result.Payload.PriceRange.Should().BeNull();
		result.Payload.TotalProductCount.Should().Be(0);
	}

	[Fact]
	public async Task Handle_WhenStringAttribute_ReturnsValueCounts()
	{
		// Arrange
		var categoryId = Guid.NewGuid();
		var category = CreateCategory("Test");

		var products = new List<Domain.Entities.Product>
		{
			CreateProductWithSkuAttributes("P1", 100m, 10, new Dictionary<string, object> { ["color"] = "Black" }),
			CreateProductWithSkuAttributes("P2", 100m, 10, new Dictionary<string, object> { ["color"] = "Black" }),
			CreateProductWithSkuAttributes("P3", 100m, 10, new Dictionary<string, object> { ["color"] = "Blue" })
		};

		var attributeDefinitions = new List<AttributeDefinition>
		{
			CreateAttributeDefinition("color", "Color", "string")
		};

		_categoryRepository.Setup(x => x.GetByIdAsync(categoryId)).ReturnsAsync(category);
		_productRepository.Setup(x => x.GetActiveByCategoryIdWithSkusAsync(categoryId)).ReturnsAsync(products);
		_attributeDefinitionRepository.Setup(x => x.GetByCodesAsync(It.IsAny<IEnumerable<string>>()))
			.ReturnsAsync(attributeDefinitions);

		var sut = CreateSut();
		var query = new GetCategoryAvailableFiltersQuery(categoryId);

		// Act
		var result = await sut.Handle(query, CancellationToken.None);

		// Assert
		result.IsSuccess.Should().BeTrue();
		var colorFilter = result.Payload!.Attributes.First();
		colorFilter.Code.Should().Be("color");
		colorFilter.AvailableValues.Should().HaveCount(2);
		
		var blackOption = colorFilter.AvailableValues!.First(v => v.Value == "Black");
		blackOption.Count.Should().Be(2);
		
		var blueOption = colorFilter.AvailableValues!.First(v => v.Value == "Blue");
		blueOption.Count.Should().Be(1);
	}

	[Fact]
	public async Task Handle_WhenNumberAttribute_ReturnsMinMaxRange()
	{
		// Arrange
		var categoryId = Guid.NewGuid();
		var category = CreateCategory("Test");

		var products = new List<Domain.Entities.Product>
		{
			CreateProductWithSkuAttributes("P1", 100m, 10, new Dictionary<string, object> { ["storage"] = 64 }),
			CreateProductWithSkuAttributes("P2", 100m, 10, new Dictionary<string, object> { ["storage"] = 128 }),
			CreateProductWithSkuAttributes("P3", 100m, 10, new Dictionary<string, object> { ["storage"] = 256 })
		};

		var attributeDefinitions = new List<AttributeDefinition>
		{
			CreateAttributeDefinition("storage", "Storage", "number")
		};

		_categoryRepository.Setup(x => x.GetByIdAsync(categoryId)).ReturnsAsync(category);
		_productRepository.Setup(x => x.GetActiveByCategoryIdWithSkusAsync(categoryId)).ReturnsAsync(products);
		_attributeDefinitionRepository.Setup(x => x.GetByCodesAsync(It.IsAny<IEnumerable<string>>()))
			.ReturnsAsync(attributeDefinitions);

		var sut = CreateSut();
		var query = new GetCategoryAvailableFiltersQuery(categoryId);

		// Act
		var result = await sut.Handle(query, CancellationToken.None);

		// Assert
		result.IsSuccess.Should().BeTrue();
		var storageFilter = result.Payload!.Attributes.First();
		storageFilter.Code.Should().Be("storage");
		storageFilter.NumberRange.Should().NotBeNull();
		storageFilter.NumberRange!.Min.Should().Be(64m);
		storageFilter.NumberRange.Max.Should().Be(256m);
		storageFilter.AvailableValues.Should().BeNull();
	}

	[Fact]
	public async Task Handle_WhenOutOfStockProducts_ExcludesFromFilters()
	{
		// Arrange
		var categoryId = Guid.NewGuid();
		var category = CreateCategory("Test");

		var products = new List<Domain.Entities.Product>
		{
			CreateProductWithSkuAttributes("P1", 100m, 10, new Dictionary<string, object> { ["color"] = "Black" }),
			CreateProductWithSkuAttributes("P2", 100m, 0, new Dictionary<string, object> { ["color"] = "Red" }) // Out of stock
		};

		var attributeDefinitions = new List<AttributeDefinition>
		{
			CreateAttributeDefinition("color", "Color", "string")
		};

		_categoryRepository.Setup(x => x.GetByIdAsync(categoryId)).ReturnsAsync(category);
		_productRepository.Setup(x => x.GetActiveByCategoryIdWithSkusAsync(categoryId)).ReturnsAsync(products);
		_attributeDefinitionRepository.Setup(x => x.GetByCodesAsync(It.IsAny<IEnumerable<string>>()))
			.ReturnsAsync(attributeDefinitions);

		var sut = CreateSut();
		var query = new GetCategoryAvailableFiltersQuery(categoryId);

		// Act
		var result = await sut.Handle(query, CancellationToken.None);

		// Assert
		result.IsSuccess.Should().BeTrue();
		var colorFilter = result.Payload!.Attributes.First();
		colorFilter.AvailableValues.Should().HaveCount(1);
		colorFilter.AvailableValues!.First().Value.Should().Be("Black");
	}

	[Fact]
	public async Task Handle_WhenRepositoryThrowsException_ReturnsFailure()
	{
		// Arrange
		var categoryId = Guid.NewGuid();
		_categoryRepository.Setup(x => x.GetByIdAsync(categoryId))
			.ThrowsAsync(new Exception("Database error"));

		var sut = CreateSut();
		var query = new GetCategoryAvailableFiltersQuery(categoryId);

		// Act
		var result = await sut.Handle(query, CancellationToken.None);

		// Assert
		result.IsSuccess.Should().BeFalse();
		result.Message.Should().Contain("Database error");
	}
}
