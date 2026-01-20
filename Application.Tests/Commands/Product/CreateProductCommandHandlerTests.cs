using Application.Commands.Product.CreateProduct;
using Application.Interfaces;
using Domain.Interfaces.Repositories;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace Application.Tests.Commands.Product;

public class CreateProductCommandHandlerTests
{
	private readonly Mock<IProductRepository> _productRepository = new();
	private readonly Mock<IStoreRepository> _storeRepository = new();
	private readonly Mock<IUserRepository> _userRepository = new();
	private readonly Mock<ICategoryRepository> _categoryRepository = new();
	private readonly Mock<ITagRepository> _tagRepository = new();
	private readonly Mock<IAttributeDefinitionRepository> _attributeDefinitionRepository = new();
	private readonly Mock<IUnitOfWork> _unitOfWork = new();
	private readonly Mock<ILogger<CreateProductCommandHandler>> _logger = new();

	private CreateProductCommandHandler CreateSut()
		=> new(
			_productRepository.Object,
			_storeRepository.Object,
			_userRepository.Object,
			_categoryRepository.Object,
			_tagRepository.Object,
			_attributeDefinitionRepository.Object,
			_unitOfWork.Object,
			_logger.Object);

	private static Domain.Entities.User CreateDomainUser(Guid identityUserId, Guid domainUserId)
	{
		var user = new Domain.Entities.User(identityUserId, email: $"user_{identityUserId:N}@example.com");
		// Domain entity IDs are set by EF; tests need a deterministic ID.
		typeof(Domain.Entities.BaseEntity<Guid>)
			.GetProperty(nameof(Domain.Entities.BaseEntity<Guid>.Id))
			?.SetValue(user, domainUserId);
		return user;
	}

	[Fact]
	public async Task Handle_WhenValidRequest_CreatesProductWithBaseSku()
	{
		// Arrange
		var identityUserId = Guid.NewGuid();
		var domainUserId = Guid.NewGuid();
		var domainUser = CreateDomainUser(identityUserId, domainUserId);
		var store = Domain.Entities.Store.Create(domainUserId, "My Store", null);
		var category = Domain.Entities.Category.Create("Electronics");
		var tag = Domain.Entities.Tag.Create("Hot");

		_userRepository.Setup(x => x.GetByIdentityUserIdAsync(identityUserId)).ReturnsAsync(domainUser);
		_storeRepository.Setup(x => x.GetByUserIdAsync(domainUserId)).ReturnsAsync(store);
		_categoryRepository.Setup(x => x.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync(category);
		_tagRepository.Setup(x => x.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync(tag);
		_unitOfWork.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

		var sut = CreateSut();
		var cmd = new CreateProductCommand(
			identityUserId,
			"iPhone",
			"desc",
			Guid.NewGuid(),
			Price: 10,
			StockQuantity: 5,
			Attributes: new Dictionary<string, object?> { ["color"] = "black" },
			TagIds: new List<Guid> { Guid.NewGuid() });

		// Act
		var res = await sut.Handle(cmd, CancellationToken.None);

		// Assert
		res.IsSuccess.Should().BeTrue();
		res.Payload.Should().NotBe(Guid.Empty);
		_productRepository.Verify(x => x.Add(It.IsAny<Domain.Entities.Product>()), Times.Once);
		_unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
	}

	[Fact]
	public async Task Handle_WhenStoreNotFound_ReturnsFailure()
	{
		// Arrange
		var identityUserId = Guid.NewGuid();
		var domainUserId = Guid.NewGuid();
		var domainUser = CreateDomainUser(identityUserId, domainUserId);
		_userRepository.Setup(x => x.GetByIdentityUserIdAsync(identityUserId)).ReturnsAsync(domainUser);
		_storeRepository.Setup(x => x.GetByUserIdAsync(domainUserId)).ReturnsAsync((Domain.Entities.Store?)null);

		var sut = CreateSut();
		var cmd = new CreateProductCommand(identityUserId, "iPhone", null, Guid.NewGuid(), 1, 0);

		// Act
		var res = await sut.Handle(cmd, CancellationToken.None);

		// Assert
		res.IsSuccess.Should().BeFalse();
		res.Message.Should().Be("Store not found");
		_productRepository.Verify(x => x.Add(It.IsAny<Domain.Entities.Product>()), Times.Never);
	}

	[Fact]
	public async Task Handle_WhenCategoryNotFound_ReturnsFailure()
	{
		// Arrange
		var identityUserId = Guid.NewGuid();
		var domainUserId = Guid.NewGuid();
		var domainUser = CreateDomainUser(identityUserId, domainUserId);
		var store = Domain.Entities.Store.Create(domainUserId, "My Store", null);
		_userRepository.Setup(x => x.GetByIdentityUserIdAsync(identityUserId)).ReturnsAsync(domainUser);
		_storeRepository.Setup(x => x.GetByUserIdAsync(domainUserId)).ReturnsAsync(store);
		_categoryRepository.Setup(x => x.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Domain.Entities.Category?)null);

		var sut = CreateSut();
		var cmd = new CreateProductCommand(identityUserId, "iPhone", null, Guid.NewGuid(), 1, 0);

		// Act
		var res = await sut.Handle(cmd, CancellationToken.None);

		// Assert
		res.IsSuccess.Should().BeFalse();
		res.Message.Should().Be("Category not found");
		_productRepository.Verify(x => x.Add(It.IsAny<Domain.Entities.Product>()), Times.Never);
	}

	[Fact]
	public async Task Handle_WhenTagNotFound_ReturnsFailure()
	{
		// Arrange
		var identityUserId = Guid.NewGuid();
		var domainUserId = Guid.NewGuid();
		var domainUser = CreateDomainUser(identityUserId, domainUserId);
		var store = Domain.Entities.Store.Create(domainUserId, "My Store", null);
		var category = Domain.Entities.Category.Create("Electronics");
		_userRepository.Setup(x => x.GetByIdentityUserIdAsync(identityUserId)).ReturnsAsync(domainUser);
		_storeRepository.Setup(x => x.GetByUserIdAsync(domainUserId)).ReturnsAsync(store);
		_categoryRepository.Setup(x => x.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync(category);
		_tagRepository.Setup(x => x.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Domain.Entities.Tag?)null);

		var sut = CreateSut();
		var cmd = new CreateProductCommand(identityUserId, "iPhone", null, Guid.NewGuid(), 1, 0, TagIds: new List<Guid> { Guid.NewGuid() });

		// Act
		var res = await sut.Handle(cmd, CancellationToken.None);

		// Assert
		res.IsSuccess.Should().BeFalse();
		res.Message.Should().Be("Tag not found");
		_productRepository.Verify(x => x.Add(It.IsAny<Domain.Entities.Product>()), Times.Never);
	}
}
