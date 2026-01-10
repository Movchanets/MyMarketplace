using Domain.Entities;
using Domain.Interfaces.Repositories;
using FluentAssertions;
using Infrastructure.Repositories;

namespace Infrastructure.IntegrationTests.Repositories;

/// <summary>
/// Інтеграційні тести для AttributeDefinitionRepository
/// </summary>
public class AttributeDefinitionRepositoryTests : TestBase
{
	private readonly IAttributeDefinitionRepository _repository;

	public AttributeDefinitionRepositoryTests()
	{
		_repository = new AttributeDefinitionRepository(DbContext);
	}

	#region AddAsync Tests

	[Fact]
	public async Task AddAsync_ShouldAddAttributeDefinitionToDatabase()
	{
		// Arrange
		var attribute = new AttributeDefinition(
			code: "color",
			name: "Color",
			dataType: "string",
			isRequired: false,
			isVariant: true,
			description: "Product color",
			unit: null,
			displayOrder: 1);

		// Act
		var result = await _repository.AddAsync(attribute);

		// Assert
		result.Should().NotBeNull();
		result.Id.Should().NotBeEmpty();
		result.Code.Should().Be("color");
		result.Name.Should().Be("Color");
		result.IsVariant.Should().BeTrue();

		var fromDb = await DbContext.AttributeDefinitions.FindAsync(result.Id);
		fromDb.Should().NotBeNull();
		fromDb!.Code.Should().Be("color");
	}

	[Fact]
	public async Task AddAsync_ShouldNormalizeCodeToLowercase()
	{
		// Arrange
		var attribute = new AttributeDefinition(
			code: "STORAGE_SIZE",
			name: "Storage Size",
			dataType: "number");

		// Act
		var result = await _repository.AddAsync(attribute);

		// Assert
		result.Code.Should().Be("storage_size");
	}

	#endregion

	#region GetByIdAsync Tests

	[Fact]
	public async Task GetByIdAsync_WithValidId_ShouldReturnAttributeDefinition()
	{
		// Arrange
		var attribute = new AttributeDefinition("size", "Size", "string", isVariant: true);
		await _repository.AddAsync(attribute);

		// Act
		var result = await _repository.GetByIdAsync(attribute.Id);

		// Assert
		result.Should().NotBeNull();
		result!.Id.Should().Be(attribute.Id);
		result.Code.Should().Be("size");
		result.Name.Should().Be("Size");
	}

	[Fact]
	public async Task GetByIdAsync_WithInvalidId_ShouldReturnNull()
	{
		// Arrange
		var nonExistentId = Guid.NewGuid();

		// Act
		var result = await _repository.GetByIdAsync(nonExistentId);

		// Assert
		result.Should().BeNull();
	}

	#endregion

	#region GetByCodeAsync Tests

	[Fact]
	public async Task GetByCodeAsync_WithValidCode_ShouldReturnAttributeDefinition()
	{
		// Arrange
		var attribute = new AttributeDefinition("material", "Material", "string");
		await _repository.AddAsync(attribute);

		// Act
		var result = await _repository.GetByCodeAsync("material");

		// Assert
		result.Should().NotBeNull();
		result!.Code.Should().Be("material");
		result.Name.Should().Be("Material");
	}

	[Fact]
	public async Task GetByCodeAsync_ShouldBeCaseInsensitive()
	{
		// Arrange
		var attribute = new AttributeDefinition("brand", "Brand", "string");
		await _repository.AddAsync(attribute);

		// Act
		var result = await _repository.GetByCodeAsync("BRAND");

		// Assert
		result.Should().NotBeNull();
		result!.Code.Should().Be("brand");
	}

	[Fact]
	public async Task GetByCodeAsync_WithInvalidCode_ShouldReturnNull()
	{
		// Act
		var result = await _repository.GetByCodeAsync("nonexistent");

		// Assert
		result.Should().BeNull();
	}

	[Fact]
	public async Task GetByCodeAsync_WithNullOrEmpty_ShouldReturnNull()
	{
		// Act & Assert
		(await _repository.GetByCodeAsync(null!)).Should().BeNull();
		(await _repository.GetByCodeAsync("")).Should().BeNull();
		(await _repository.GetByCodeAsync("   ")).Should().BeNull();
	}

	#endregion

	#region GetAllAsync Tests

	[Fact]
	public async Task GetAllAsync_ShouldReturnOnlyActiveByDefault()
	{
		// Arrange
		var active1 = new AttributeDefinition("attr1", "Attribute 1", displayOrder: 2);
		var active2 = new AttributeDefinition("attr2", "Attribute 2", displayOrder: 1);
		var inactive = new AttributeDefinition("attr3", "Attribute 3");
		inactive.Deactivate();

		await _repository.AddAsync(active1);
		await _repository.AddAsync(active2);
		await _repository.AddAsync(inactive);

		// Act
		var result = (await _repository.GetAllAsync()).ToList();

		// Assert
		result.Should().HaveCount(2);
		result.Should().Contain(a => a.Code == "attr1");
		result.Should().Contain(a => a.Code == "attr2");
		result.Should().NotContain(a => a.Code == "attr3");
	}

	[Fact]
	public async Task GetAllAsync_WithIncludeInactive_ShouldReturnAll()
	{
		// Arrange
		var active = new AttributeDefinition("active_attr", "Active");
		var inactive = new AttributeDefinition("inactive_attr", "Inactive");
		inactive.Deactivate();

		await _repository.AddAsync(active);
		await _repository.AddAsync(inactive);

		// Act
		var result = (await _repository.GetAllAsync(includeInactive: true)).ToList();

		// Assert
		result.Should().HaveCount(2);
		result.Should().Contain(a => a.Code == "active_attr");
		result.Should().Contain(a => a.Code == "inactive_attr");
	}

	[Fact]
	public async Task GetAllAsync_ShouldOrderByDisplayOrderThenByName()
	{
		// Arrange
		var attr1 = new AttributeDefinition("z_attr", "Z Attribute", displayOrder: 1);
		var attr2 = new AttributeDefinition("a_attr", "A Attribute", displayOrder: 1);
		var attr3 = new AttributeDefinition("m_attr", "M Attribute", displayOrder: 0);

		await _repository.AddAsync(attr1);
		await _repository.AddAsync(attr2);
		await _repository.AddAsync(attr3);

		// Act
		var result = (await _repository.GetAllAsync()).ToList();

		// Assert
		result.Should().HaveCount(3);
		result[0].Code.Should().Be("m_attr"); // displayOrder: 0
		result[1].Code.Should().Be("a_attr"); // displayOrder: 1, name: A
		result[2].Code.Should().Be("z_attr"); // displayOrder: 1, name: Z
	}

	#endregion

	#region GetVariantAttributesAsync Tests

	[Fact]
	public async Task GetVariantAttributesAsync_ShouldReturnOnlyVariantAttributes()
	{
		// Arrange
		var variant1 = new AttributeDefinition("color", "Color", isVariant: true);
		var variant2 = new AttributeDefinition("size", "Size", isVariant: true);
		var nonVariant = new AttributeDefinition("brand", "Brand", isVariant: false);

		await _repository.AddAsync(variant1);
		await _repository.AddAsync(variant2);
		await _repository.AddAsync(nonVariant);

		// Act
		var result = (await _repository.GetVariantAttributesAsync()).ToList();

		// Assert
		result.Should().HaveCount(2);
		result.Should().Contain(a => a.Code == "color");
		result.Should().Contain(a => a.Code == "size");
		result.Should().NotContain(a => a.Code == "brand");
	}

	[Fact]
	public async Task GetVariantAttributesAsync_ShouldNotReturnInactiveVariants()
	{
		// Arrange
		var activeVariant = new AttributeDefinition("active_variant", "Active", isVariant: true);
		var inactiveVariant = new AttributeDefinition("inactive_variant", "Inactive", isVariant: true);
		inactiveVariant.Deactivate();

		await _repository.AddAsync(activeVariant);
		await _repository.AddAsync(inactiveVariant);

		// Act
		var result = (await _repository.GetVariantAttributesAsync()).ToList();

		// Assert
		result.Should().HaveCount(1);
		result[0].Code.Should().Be("active_variant");
	}

	#endregion

	#region GetByCodesAsync Tests

	[Fact]
	public async Task GetByCodesAsync_ShouldReturnMatchingAttributes()
	{
		// Arrange
		var attr1 = new AttributeDefinition("color", "Color");
		var attr2 = new AttributeDefinition("size", "Size");
		var attr3 = new AttributeDefinition("brand", "Brand");

		await _repository.AddAsync(attr1);
		await _repository.AddAsync(attr2);
		await _repository.AddAsync(attr3);

		// Act
		var result = (await _repository.GetByCodesAsync(new[] { "color", "brand" })).ToList();

		// Assert
		result.Should().HaveCount(2);
		result.Should().Contain(a => a.Code == "color");
		result.Should().Contain(a => a.Code == "brand");
	}

	[Fact]
	public async Task GetByCodesAsync_ShouldBeCaseInsensitive()
	{
		// Arrange
		var attr = new AttributeDefinition("storage", "Storage");
		await _repository.AddAsync(attr);

		// Act
		var result = (await _repository.GetByCodesAsync(new[] { "STORAGE", "Storage", "storage" })).ToList();

		// Assert
		result.Should().HaveCount(1);
		result[0].Code.Should().Be("storage");
	}

	[Fact]
	public async Task GetByCodesAsync_WithEmptyList_ShouldReturnEmpty()
	{
		// Act
		var result = await _repository.GetByCodesAsync(Array.Empty<string>());

		// Assert
		result.Should().BeEmpty();
	}

	#endregion

	#region UpdateAsync Tests

	[Fact]
	public async Task UpdateAsync_ShouldUpdateAttributeDefinition()
	{
		// Arrange
		var attribute = new AttributeDefinition("weight", "Weight", "number", unit: "kg");
		await _repository.AddAsync(attribute);

		// Act
		attribute.Update(
			name: "Product Weight",
			dataType: "number",
			isRequired: true,
			isVariant: false,
			description: "Weight in grams",
			unit: "g",
			displayOrder: 5);

		await _repository.UpdateAsync(attribute);

		// Assert
		var fromDb = await _repository.GetByIdAsync(attribute.Id);
		fromDb.Should().NotBeNull();
		fromDb!.Name.Should().Be("Product Weight");
		fromDb.IsRequired.Should().BeTrue();
		fromDb.Unit.Should().Be("g");
		fromDb.DisplayOrder.Should().Be(5);
		fromDb.Description.Should().Be("Weight in grams");
	}

	[Fact]
	public async Task UpdateAsync_ShouldUpdateAllowedValues()
	{
		// Arrange
		var attribute = new AttributeDefinition("color", "Color");
		await _repository.AddAsync(attribute);

		// Act
		attribute.SetAllowedValues(new[] { "Red", "Green", "Blue" });
		await _repository.UpdateAsync(attribute);

		// Assert
		var fromDb = await _repository.GetByIdAsync(attribute.Id);
		fromDb.Should().NotBeNull();
		fromDb!.AllowedValues.Should().NotBeNull();

		var values = fromDb.GetAllowedValuesList();
		values.Should().HaveCount(3);
		values.Should().Contain("Red");
		values.Should().Contain("Green");
		values.Should().Contain("Blue");
	}

	#endregion

	#region DeleteAsync Tests

	[Fact]
	public async Task DeleteAsync_ShouldRemoveAttributeDefinition()
	{
		// Arrange
		var attribute = new AttributeDefinition("temp_attr", "Temporary");
		await _repository.AddAsync(attribute);
		var id = attribute.Id;

		// Act
		await _repository.DeleteAsync(id);

		// Assert
		var fromDb = await _repository.GetByIdAsync(id);
		fromDb.Should().BeNull();
	}

	[Fact]
	public async Task DeleteAsync_WithNonExistentId_ShouldNotThrow()
	{
		// Arrange
		var nonExistentId = Guid.NewGuid();

		// Act
		var act = async () => await _repository.DeleteAsync(nonExistentId);

		// Assert
		await act.Should().NotThrowAsync();
	}

	#endregion

	#region ExistsAsync Tests

	[Fact]
	public async Task ExistsAsync_WithExistingCode_ShouldReturnTrue()
	{
		// Arrange
		var attribute = new AttributeDefinition("existing", "Existing");
		await _repository.AddAsync(attribute);

		// Act
		var result = await _repository.ExistsAsync("existing");

		// Assert
		result.Should().BeTrue();
	}

	[Fact]
	public async Task ExistsAsync_WithNonExistingCode_ShouldReturnFalse()
	{
		// Act
		var result = await _repository.ExistsAsync("nonexisting");

		// Assert
		result.Should().BeFalse();
	}

	[Fact]
	public async Task ExistsAsync_ShouldBeCaseInsensitive()
	{
		// Arrange
		var attribute = new AttributeDefinition("mycode", "My Code");
		await _repository.AddAsync(attribute);

		// Act & Assert
		(await _repository.ExistsAsync("MYCODE")).Should().BeTrue();
		(await _repository.ExistsAsync("MyCode")).Should().BeTrue();
		(await _repository.ExistsAsync("mycode")).Should().BeTrue();
	}

	[Fact]
	public async Task ExistsAsync_WithNullOrEmpty_ShouldReturnFalse()
	{
		// Act & Assert
		(await _repository.ExistsAsync(null!)).Should().BeFalse();
		(await _repository.ExistsAsync("")).Should().BeFalse();
		(await _repository.ExistsAsync("   ")).Should().BeFalse();
	}

	#endregion

	#region AllowedValues Tests

	[Fact]
	public async Task AllowedValues_ShouldPersistAndRetrieveCorrectly()
	{
		// Arrange
		var attribute = new AttributeDefinition("size", "Size");
		attribute.SetAllowedValues(new[] { "XS", "S", "M", "L", "XL", "XXL" });

		// Act
		await _repository.AddAsync(attribute);
		var fromDb = await _repository.GetByIdAsync(attribute.Id);

		// Assert
		fromDb.Should().NotBeNull();
		var values = fromDb!.GetAllowedValuesList();
		values.Should().HaveCount(6);
		values.Should().ContainInOrder("XS", "S", "M", "L", "XL", "XXL");
	}

	[Fact]
	public async Task AllowedValues_ClearingValues_ShouldPersistNull()
	{
		// Arrange
		var attribute = new AttributeDefinition("capacity", "Capacity");
		attribute.SetAllowedValues(new[] { "16GB", "32GB", "64GB" });
		await _repository.AddAsync(attribute);

		// Act
		attribute.SetAllowedValues(null);
		await _repository.UpdateAsync(attribute);

		// Assert
		var fromDb = await _repository.GetByIdAsync(attribute.Id);
		fromDb.Should().NotBeNull();
		fromDb!.AllowedValues.Should().BeNull();
		var values = fromDb.GetAllowedValuesList();
		(values == null || values.Count == 0).Should().BeTrue();
	}

	#endregion

	#region Activate/Deactivate Tests

	[Fact]
	public async Task Deactivate_ShouldSetIsActiveToFalse()
	{
		// Arrange
		var attribute = new AttributeDefinition("active_test", "Active Test");
		await _repository.AddAsync(attribute);

		// Act
		attribute.Deactivate();
		await _repository.UpdateAsync(attribute);

		// Assert
		var fromDb = await _repository.GetByIdAsync(attribute.Id);
		fromDb.Should().NotBeNull();
		fromDb!.IsActive.Should().BeFalse();
	}

	[Fact]
	public async Task Activate_ShouldSetIsActiveToTrue()
	{
		// Arrange
		var attribute = new AttributeDefinition("inactive_test", "Inactive Test");
		attribute.Deactivate();
		await _repository.AddAsync(attribute);

		// Act
		attribute.Activate();
		await _repository.UpdateAsync(attribute);

		// Assert
		var fromDb = await _repository.GetByIdAsync(attribute.Id);
		fromDb.Should().NotBeNull();
		fromDb!.IsActive.Should().BeTrue();
	}

	#endregion
}
