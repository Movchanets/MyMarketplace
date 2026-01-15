using Application.Commands.Category.CreateCategory;
using FluentAssertions;
using Infrastructure.Repositories;
using Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;

namespace Infrastructure.IntegrationTests.Commands.Category;

public class CreateCategoryCommandHandlerIntegrationTests : TestBase
{
	[Fact]
	public async Task Handle_ShouldCreateCategoryInDatabase()
	{
		// Arrange
		var repo = new CategoryRepository(DbContext);
		var uow = new UnitOfWork(DbContext);
		var cache = new MemoryCache(new MemoryCacheOptions());
		var handler = new CreateCategoryCommandHandler(repo, uow, cache, NullLogger<CreateCategoryCommandHandler>.Instance);

		var command = new CreateCategoryCommand("Electronics", "desc", null);

		// Act
		var result = await handler.Handle(command, CancellationToken.None);

		// Assert
		result.IsSuccess.Should().BeTrue();
		result.Payload.Should().NotBe(Guid.Empty);

		var created = await DbContext.Categories.FirstOrDefaultAsync(c => c.Id == result.Payload);
		created.Should().NotBeNull();
		created!.Name.Should().Be("Electronics");
		created.Description.Should().Be("desc");
		created.ParentCategoryId.Should().BeNull();
	}

	[Fact]
	public async Task Handle_WhenParentCategoryMissing_ShouldReturnFailure()
	{
		// Arrange
		var repo = new CategoryRepository(DbContext);
		var uow = new UnitOfWork(DbContext);
		var cache = new MemoryCache(new MemoryCacheOptions());
		var handler = new CreateCategoryCommandHandler(repo, uow, cache, NullLogger<CreateCategoryCommandHandler>.Instance);

		var command = new CreateCategoryCommand("Phones", null, null, Guid.NewGuid());

		// Act
		var result = await handler.Handle(command, CancellationToken.None);

		// Assert
		result.IsSuccess.Should().BeFalse();
		result.Message.Should().Be("Parent category not found");
		(await DbContext.Categories.CountAsync()).Should().Be(0);
	}

	[Fact]
	public async Task Handle_WhenSlugAlreadyExists_ShouldReturnFailure()
	{
		// Arrange
		DbContext.Categories.Add(Domain.Entities.Category.Create("Existing"));
		await DbContext.SaveChangesAsync();

		var repo = new CategoryRepository(DbContext);
		var uow = new UnitOfWork(DbContext);
		var cache = new MemoryCache(new MemoryCacheOptions());
		var handler = new CreateCategoryCommandHandler(repo, uow, cache, NullLogger<CreateCategoryCommandHandler>.Instance);

		var command = new CreateCategoryCommand("Existing", null, null);

		// Act
		var result = await handler.Handle(command, CancellationToken.None);

		// Assert
		result.IsSuccess.Should().BeFalse();
		result.Message.Should().Be("Category with same slug already exists");
		(await DbContext.Categories.CountAsync()).Should().Be(1);
	}
}
