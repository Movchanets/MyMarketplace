using Application.Commands.Category.UpdateCategory;
using Application.Interfaces;
using Domain.Interfaces.Repositories;
using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;

namespace Application.Tests.Commands.Category;

public class UpdateCategoryCommandHandlerTests
{
	private readonly Mock<ICategoryRepository> _categoryRepository = new();
	private readonly Mock<IUnitOfWork> _unitOfWork = new();
	private readonly Mock<IMemoryCache> _cache = new();
	private readonly Mock<ILogger<UpdateCategoryCommandHandler>> _logger = new();

	private UpdateCategoryCommandHandler CreateSut()
		=> new(_categoryRepository.Object, _unitOfWork.Object, _cache.Object, _logger.Object);

	[Fact]
	public async Task Handle_WhenCategoryNotFound_ReturnsFailure()
	{
		// Arrange
		var id = Guid.NewGuid();
		_categoryRepository.Setup(x => x.GetByIdAsync(id)).ReturnsAsync((Domain.Entities.Category?)null);
		var sut = CreateSut();

		// Act
		var res = await sut.Handle(new UpdateCategoryCommand(id, "NewName"), CancellationToken.None);

		// Assert
		res.IsSuccess.Should().BeFalse();
		res.Message.Should().Be("Category not found");
		_unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
	}

	[Fact]
	public async Task Handle_WhenSlugAlreadyExistsForDifferentCategory_ReturnsFailure()
	{
		// Arrange
		var existing = Domain.Entities.Category.Create("Existing");
		var other = Domain.Entities.Category.Create("Other");

		_categoryRepository.Setup(x => x.GetByIdAsync(existing.Id)).ReturnsAsync(existing);
		_categoryRepository.Setup(x => x.GetBySlugAsync(It.IsAny<string>())).ReturnsAsync(other);

		var sut = CreateSut();

		// Act
		var res = await sut.Handle(new UpdateCategoryCommand(existing.Id, "Other"), CancellationToken.None);

		// Assert
		res.IsSuccess.Should().BeFalse();
		res.Message.Should().Be("Category with same slug already exists");
		_unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
	}

	[Fact]
	public async Task Handle_WhenParentNotFound_ReturnsFailure()
	{
		// Arrange
		var category = Domain.Entities.Category.Create("Cat");
		var parentId = Guid.NewGuid();

		_categoryRepository.Setup(x => x.GetByIdAsync(category.Id)).ReturnsAsync(category);
		_categoryRepository.Setup(x => x.GetBySlugAsync(It.IsAny<string>())).ReturnsAsync((Domain.Entities.Category?)null);
		_categoryRepository.Setup(x => x.GetByIdAsync(parentId)).ReturnsAsync((Domain.Entities.Category?)null);

		var sut = CreateSut();

		// Act
		var res = await sut.Handle(new UpdateCategoryCommand(category.Id, "Cat2", null, null, parentId), CancellationToken.None);

		// Assert
		res.IsSuccess.Should().BeFalse();
		res.Message.Should().Be("Parent category not found");
		_unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
	}

	[Fact]
	public async Task Handle_WhenValid_UpdatesCategoryAndInvalidatesCache()
	{
		// Arrange
		var category = Domain.Entities.Category.Create("Cat");
		_categoryRepository.Setup(x => x.GetByIdAsync(category.Id)).ReturnsAsync(category);
		_categoryRepository.Setup(x => x.GetBySlugAsync(It.IsAny<string>())).ReturnsAsync((Domain.Entities.Category?)null);
		_unitOfWork.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

		var sut = CreateSut();

		// Act
		var res = await sut.Handle(new UpdateCategoryCommand(category.Id, "Cat New", "desc", null), CancellationToken.None);

		// Assert
		res.IsSuccess.Should().BeTrue();
		_categoryRepository.Verify(x => x.Update(It.IsAny<Domain.Entities.Category>()), Times.Once);
		_unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
		_cache.Verify(x => x.Remove("categories:all"), Times.Once);
		_cache.Verify(x => x.Remove("categories:top-level"), Times.Once);
	}
}
