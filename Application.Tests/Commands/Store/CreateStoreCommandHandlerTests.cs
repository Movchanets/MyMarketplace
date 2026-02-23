using Application.Commands.Store.CreateStore;
using Application.Interfaces;
using Domain.Interfaces.Repositories;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace Application.Tests.Commands.Store;

public class CreateStoreCommandHandlerTests
{
	private readonly Mock<IStoreRepository> _storeRepository = new();
	private readonly Mock<IUserRepository> _userRepository = new();
	private readonly Mock<IUnitOfWork> _unitOfWork = new();
	private readonly Mock<ILogger<CreateStoreCommandHandler>> _logger = new();

	private CreateStoreCommandHandler CreateSut()
		=> new(_storeRepository.Object, _userRepository.Object, _unitOfWork.Object, _logger.Object);

	[Fact]
	public async Task Handle_WhenValidRequest_CreatesStore()
	{
		// Arrange
		var identityUserId = Guid.NewGuid();
		var domainUserId = Guid.NewGuid();
		var domainUser = new Domain.Entities.User(Guid.NewGuid(), email: "test@example.com");
		typeof(Domain.Entities.User).GetProperty(nameof(Domain.Entities.User.Id))!.SetValue(domainUser, domainUserId);

		_userRepository
			.Setup(x => x.GetByIdentityUserIdAsync(identityUserId))
			.ReturnsAsync(domainUser);

		_storeRepository
			.Setup(x => x.GetByUserIdAsync(domainUserId))
			.ReturnsAsync((Domain.Entities.Store?)null);

		_storeRepository
			.Setup(x => x.GetBySlugAsync(It.IsAny<string>()))
			.ReturnsAsync((Domain.Entities.Store?)null);

		_unitOfWork
			.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
			.ReturnsAsync(1);

		var sut = CreateSut();
		var cmd = new CreateStoreCommand(identityUserId, "My Store", "desc");

		// Act
		var res = await sut.Handle(cmd, CancellationToken.None);

		// Assert
		res.IsSuccess.Should().BeTrue();
		res.Payload.Should().NotBe(Guid.Empty);

		_storeRepository.Verify(x => x.Add(It.IsAny<Domain.Entities.Store>()), Times.Once);
		_unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
	}

	[Fact]
	public async Task Handle_WhenUserNotFound_ReturnsFailure()
	{
		// Arrange
		var identityUserId = Guid.NewGuid();
		_userRepository
			.Setup(x => x.GetByIdentityUserIdAsync(identityUserId))
			.ReturnsAsync((Domain.Entities.User?)null);

		var sut = CreateSut();
		var cmd = new CreateStoreCommand(identityUserId, "My Store", null);

		// Act
		var res = await sut.Handle(cmd, CancellationToken.None);

		// Assert
		res.IsSuccess.Should().BeFalse();
		res.Message.Should().Be("User not found");

		_storeRepository.Verify(x => x.Add(It.IsAny<Domain.Entities.Store>()), Times.Never);
		_unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
	}

	[Fact]
	public async Task Handle_WhenUserAlreadyHasStore_ReturnsFailure()
	{
		// Arrange
		var identityUserId = Guid.NewGuid();
		var domainUserId = Guid.NewGuid();
		var domainUser = new Domain.Entities.User(Guid.NewGuid(), email: "test@example.com");
		typeof(Domain.Entities.User).GetProperty(nameof(Domain.Entities.User.Id))!.SetValue(domainUser, domainUserId);

		_userRepository
			.Setup(x => x.GetByIdentityUserIdAsync(identityUserId))
			.ReturnsAsync(domainUser);

		_storeRepository
			.Setup(x => x.GetByUserIdAsync(domainUserId))
			.ReturnsAsync(Domain.Entities.Store.Create(domainUserId, "Existing", null));

		var sut = CreateSut();
		var cmd = new CreateStoreCommand(identityUserId, "My Store", null);

		// Act
		var res = await sut.Handle(cmd, CancellationToken.None);

		// Assert
		res.IsSuccess.Should().BeFalse();
		res.Message.Should().Be("User already has a store");

		_storeRepository.Verify(x => x.Add(It.IsAny<Domain.Entities.Store>()), Times.Never);
		_unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
	}

	[Fact]
	public async Task Handle_WhenSlugAlreadyExists_ReturnsFailure()
	{
		// Arrange
		var identityUserId = Guid.NewGuid();
		var domainUserId = Guid.NewGuid();
		var domainUser = new Domain.Entities.User(Guid.NewGuid(), email: "test@example.com");
		typeof(Domain.Entities.User).GetProperty(nameof(Domain.Entities.User.Id))!.SetValue(domainUser, domainUserId);

		_userRepository
			.Setup(x => x.GetByIdentityUserIdAsync(identityUserId))
			.ReturnsAsync(domainUser);

		_storeRepository
			.Setup(x => x.GetByUserIdAsync(domainUserId))
			.ReturnsAsync((Domain.Entities.Store?)null);

		_storeRepository
			.Setup(x => x.GetBySlugAsync(It.IsAny<string>()))
			.ReturnsAsync(Domain.Entities.Store.Create(Guid.NewGuid(), "My Store", null));

		var sut = CreateSut();
		var cmd = new CreateStoreCommand(identityUserId, "My Store", null);

		// Act
		var res = await sut.Handle(cmd, CancellationToken.None);

		// Assert
		res.IsSuccess.Should().BeFalse();
		res.Message.Should().Be("Store with same slug already exists");

		_storeRepository.Verify(x => x.Add(It.IsAny<Domain.Entities.Store>()), Times.Never);
		_unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
	}
}
