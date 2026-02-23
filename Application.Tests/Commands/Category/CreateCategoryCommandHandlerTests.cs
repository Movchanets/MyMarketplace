using Application.Commands.Category.CreateCategory;
using Application.Interfaces;
using Domain.Interfaces.Repositories;
using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;

namespace Application.Tests.Commands.Category;

public class CreateCategoryCommandHandlerTests
{
	private readonly Mock<ICategoryRepository> _categoryRepository = new();
	private readonly Mock<IUnitOfWork> _unitOfWork = new();
	private readonly Mock<IMemoryCache> _cache = new();
	private readonly Mock<ILogger<CreateCategoryCommandHandler>> _logger = new();

	private CreateCategoryCommandHandler CreateSut()
		=> new(_categoryRepository.Object, _unitOfWork.Object, _cache.Object, _logger.Object);

	[Fact]
	public async Task Handle_WhenValidRequest_CreatesCategoryAndInvalidatesCache()
	{
		// Arrange
		_categoryRepository
			.Setup(x => x.GetBySlugAsync(It.IsAny<string>()))
			.ReturnsAsync((Domain.Entities.Category?)null);

		_unitOfWork
			.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
			.ReturnsAsync(1);

		var sut = CreateSut();
		var cmd = new CreateCategoryCommand("Electronics", "desc", null);

		// Act
		var res = await sut.Handle(cmd, CancellationToken.None);

		// Assert
		res.IsSuccess.Should().BeTrue();
		res.Payload.Should().NotBe(Guid.Empty);

		_categoryRepository.Verify(x => x.Add(It.IsAny<Domain.Entities.Category>()), Times.Once);
		_unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

		_cache.Verify(x => x.Remove("categories:all"), Times.Once);
		_cache.Verify(x => x.Remove("categories:top-level"), Times.Once);
	}

	[Fact]
	public async Task Handle_WhenParentNotFound_ReturnsFailure()
	{
		// Arrange
		var parentId = Guid.NewGuid();
		_categoryRepository
			.Setup(x => x.GetByIdAsync(parentId))
			.ReturnsAsync((Domain.Entities.Category?)null);

		var sut = CreateSut();
		var cmd = new CreateCategoryCommand("Phones", null, null, parentId);

		// Act
		var res = await sut.Handle(cmd, CancellationToken.None);

		// Assert
		res.IsSuccess.Should().BeFalse();
		res.Message.Should().Be("Parent category not found");

		_categoryRepository.Verify(x => x.Add(It.IsAny<Domain.Entities.Category>()), Times.Never);
		_unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
		_cache.Verify(x => x.Remove(It.IsAny<object>()), Times.Never);
	}

	[Fact]
	public async Task Handle_WhenSlugAlreadyExists_ReturnsFailure()
	{
		// Arrange
		var existing = Domain.Entities.Category.Create("Existing");
		_categoryRepository
			.Setup(x => x.GetBySlugAsync(It.IsAny<string>()))
			.ReturnsAsync(existing);

		var sut = CreateSut();
		var cmd = new CreateCategoryCommand("Existing", null, null);

		// Act
		var res = await sut.Handle(cmd, CancellationToken.None);

		// Assert
		res.IsSuccess.Should().BeFalse();
		res.Message.Should().Be("Category with same slug already exists");

		_categoryRepository.Verify(x => x.Add(It.IsAny<Domain.Entities.Category>()), Times.Never);
		_unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
		_cache.Verify(x => x.Remove(It.IsAny<object>()), Times.Never);
	}
}
