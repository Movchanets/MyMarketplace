using Application.Behaviors;
using Application.Commands.Category.CreateCategory;
using Application.Commands.Category.DeleteCategory;
using Application.Commands.Category.UpdateCategory;
using Application.Commands.Product.CreateProduct;
using Application.Commands.Product.DeleteProduct;
using Application.Commands.Product.UpdateProduct;
using Application.Commands.Tag.CreateTag;
using Application.Commands.Tag.DeleteTag;
using Application.Commands.Tag.UpdateTag;
using FluentAssertions;
using Xunit;

namespace Application.Tests.Behaviors;

public class CacheInvalidatingCommandsTests
{
	[Fact]
	public void CreateCategoryCommand_ShouldImplementICacheInvalidatingCommand()
	{
		// Arrange
		var command = new CreateCategoryCommand("Test Category");

		// Act & Assert
		command.Should().BeAssignableTo<ICacheInvalidatingCommand>();
		command.CacheTags.Should().Contain("categories");
	}

	[Fact]
	public void UpdateCategoryCommand_ShouldImplementICacheInvalidatingCommand()
	{
		// Arrange
		var command = new UpdateCategoryCommand(Guid.NewGuid(), "Updated Category");

		// Act & Assert
		command.Should().BeAssignableTo<ICacheInvalidatingCommand>();
		command.CacheTags.Should().Contain("categories");
	}

	[Fact]
	public void DeleteCategoryCommand_ShouldInvalidateCategoriesAndProducts()
	{
		// Arrange
		var command = new DeleteCategoryCommand(Guid.NewGuid());

		// Act & Assert
		command.Should().BeAssignableTo<ICacheInvalidatingCommand>();
		command.CacheTags.Should().Contain("categories");
		command.CacheTags.Should().Contain("products");
	}

	[Fact]
	public void CreateTagCommand_ShouldImplementICacheInvalidatingCommand()
	{
		// Arrange
		var command = new CreateTagCommand("Test Tag");

		// Act & Assert
		command.Should().BeAssignableTo<ICacheInvalidatingCommand>();
		command.CacheTags.Should().Contain("tags");
	}

	[Fact]
	public void UpdateTagCommand_ShouldImplementICacheInvalidatingCommand()
	{
		// Arrange
		var command = new UpdateTagCommand(Guid.NewGuid(), "Updated Tag");

		// Act & Assert
		command.Should().BeAssignableTo<ICacheInvalidatingCommand>();
		command.CacheTags.Should().Contain("tags");
	}

	[Fact]
	public void DeleteTagCommand_ShouldInvalidateTagsAndProducts()
	{
		// Arrange
		var command = new DeleteTagCommand(Guid.NewGuid());

		// Act & Assert
		command.Should().BeAssignableTo<ICacheInvalidatingCommand>();
		command.CacheTags.Should().Contain("tags");
		command.CacheTags.Should().Contain("products");
	}

	[Fact]
	public void CreateProductCommand_ShouldImplementICacheInvalidatingCommand()
	{
		// Arrange
		var command = new CreateProductCommand(
			Guid.NewGuid(),
			"Test Product",
			"Description",
			new List<Guid> { Guid.NewGuid() },
			100m,
			10);

		// Act & Assert
		command.Should().BeAssignableTo<ICacheInvalidatingCommand>();
		command.CacheTags.Should().Contain("products");
	}

	[Fact]
	public void UpdateProductCommand_ShouldImplementICacheInvalidatingCommand()
	{
		// Arrange
		var command = new UpdateProductCommand(
			Guid.NewGuid(),
			Guid.NewGuid(),
			"Updated Product",
			"Description",
			new List<Guid> { Guid.NewGuid() });

		// Act & Assert
		command.Should().BeAssignableTo<ICacheInvalidatingCommand>();
		command.CacheTags.Should().Contain("products");
	}

	[Fact]
	public void DeleteProductCommand_ShouldImplementICacheInvalidatingCommand()
	{
		// Arrange
		var command = new DeleteProductCommand(Guid.NewGuid(), Guid.NewGuid());

		// Act & Assert
		command.Should().BeAssignableTo<ICacheInvalidatingCommand>();
		command.CacheTags.Should().Contain("products");
	}
}
