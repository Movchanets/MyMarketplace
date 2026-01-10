using Application.Commands.Product.DeleteProduct;
using Application.Interfaces;
using Domain.Interfaces.Repositories;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace Application.Tests.Commands.Product;

public class DeleteProductCommandHandlerTests
{
	private readonly Mock<IProductRepository> _productRepository = new();
	private readonly Mock<IStoreRepository> _storeRepository = new();
	private readonly Mock<IUserRepository> _userRepository = new();
	private readonly Mock<IUnitOfWork> _unitOfWork = new();
	private readonly Mock<ILogger<DeleteProductCommandHandler>> _logger = new();

	private DeleteProductCommandHandler CreateSut()
		=> new(_productRepository.Object, _storeRepository.Object, _userRepository.Object, _unitOfWork.Object, _logger.Object);

	[Fact]
	public async Task Handle_WhenUserNotFound_ReturnsFailure()
	{
		// Arrange
		var identityUserId = Guid.NewGuid();
		_userRepository.Setup(x => x.GetByIdentityUserIdAsync(identityUserId)).ReturnsAsync((Domain.Entities.User?)null);

		var sut = CreateSut();
		var cmd = new DeleteProductCommand(identityUserId, Guid.NewGuid());

		// Act
		var res = await sut.Handle(cmd, CancellationToken.None);

		// Assert
		res.IsSuccess.Should().BeFalse();
		res.Message.Should().Be("User not found");
		_productRepository.Verify(x => x.Delete(It.IsAny<Domain.Entities.Product>()), Times.Never);
	}

	[Fact]
	public async Task Handle_WhenStoreNotFound_ReturnsFailure()
	{
		// Arrange
		var identityUserId = Guid.NewGuid();
		var domainUserId = Guid.NewGuid();
		var domainUser = new Domain.Entities.User(identityUserId, "Test", "User", "test@test.com");
		// Manually set Id since EF Core isn't managing it in unit tests
		typeof(Domain.Entities.User).GetProperty("Id")?.SetValue(domainUser, domainUserId);

		_userRepository.Setup(x => x.GetByIdentityUserIdAsync(identityUserId)).ReturnsAsync(domainUser);
		_storeRepository.Setup(x => x.GetByUserIdAsync(domainUserId)).ReturnsAsync((Domain.Entities.Store?)null);

		var sut = CreateSut();
		var cmd = new DeleteProductCommand(identityUserId, Guid.NewGuid());

		// Act
		var res = await sut.Handle(cmd, CancellationToken.None);

		// Assert
		res.IsSuccess.Should().BeFalse();
		res.Message.Should().Be("Store not found");
		_productRepository.Verify(x => x.Delete(It.IsAny<Domain.Entities.Product>()), Times.Never);
	}

	[Fact]
	public async Task Handle_WhenValidRequest_DeletesProduct()
	{
		// Arrange
		var identityUserId = Guid.NewGuid();
		var domainUserId = Guid.NewGuid();
		var domainUser = new Domain.Entities.User(identityUserId, "Test", "User", "test@test.com");
		// Manually set Id since EF Core isn't managing it in unit tests
		typeof(Domain.Entities.User).GetProperty("Id")?.SetValue(domainUser, domainUserId);

		var store = Domain.Entities.Store.Create(domainUserId, "My Store", null);
		var product = new Domain.Entities.Product("Old");
		store.AddProduct(product);

		_userRepository.Setup(x => x.GetByIdentityUserIdAsync(identityUserId)).ReturnsAsync(domainUser);
		_storeRepository.Setup(x => x.GetByUserIdAsync(domainUserId)).ReturnsAsync(store);
		_productRepository.Setup(x => x.GetByIdAsync(product.Id)).ReturnsAsync(product);
		_unitOfWork.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

		var sut = CreateSut();
		var cmd = new DeleteProductCommand(identityUserId, product.Id);

		// Act
		var res = await sut.Handle(cmd, CancellationToken.None);

		// Assert
		res.IsSuccess.Should().BeTrue();
		res.Message.Should().Be("Product deleted successfully");
		_productRepository.Verify(x => x.Delete(product), Times.Once);
		_unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
	}
}
