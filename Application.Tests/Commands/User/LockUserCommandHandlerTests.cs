using Application.Commands.User.LockUser;
using Application.Interfaces;
using FluentAssertions;
using Moq;

namespace Application.Tests.Commands.User;

public class LockUserCommandHandlerTests
{
    private readonly Mock<IAdminUserService> _adminUserService = new();

    private LockUserCommandHandler CreateSut() => new(_adminUserService.Object);

    [Fact]
    public async Task Handle_WhenLockingUser_ReturnsSuccess()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _adminUserService
            .Setup(x => x.SetUserLockoutAsync(userId, true, null))
            .ReturnsAsync((true, "User locked successfully"));

        var sut = CreateSut();
        var cmd = new LockUserCommand(userId, true, null);

        // Act
        var result = await sut.Handle(cmd, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Message.Should().Be("User locked successfully");
    }

    [Fact]
    public async Task Handle_WhenUnlockingUser_ReturnsSuccess()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _adminUserService
            .Setup(x => x.SetUserLockoutAsync(userId, false, null))
            .ReturnsAsync((true, "User unlocked successfully"));

        var sut = CreateSut();
        var cmd = new LockUserCommand(userId, false, null);

        // Act
        var result = await sut.Handle(cmd, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Message.Should().Be("User unlocked successfully");
    }

    [Fact]
    public async Task Handle_WhenLockingWithExpiration_PassesLockUntilDate()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var lockUntil = DateTime.UtcNow.AddDays(7);

        _adminUserService
            .Setup(x => x.SetUserLockoutAsync(userId, true, lockUntil))
            .ReturnsAsync((true, "User locked until " + lockUntil));

        var sut = CreateSut();
        var cmd = new LockUserCommand(userId, true, lockUntil);

        // Act
        var result = await sut.Handle(cmd, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _adminUserService.Verify(x => x.SetUserLockoutAsync(userId, true, lockUntil), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenUserNotFound_ReturnsFailure()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _adminUserService
            .Setup(x => x.SetUserLockoutAsync(userId, It.IsAny<bool>(), It.IsAny<DateTime?>()))
            .ReturnsAsync((false, "User not found"));

        var sut = CreateSut();
        var cmd = new LockUserCommand(userId, true, null);

        // Act
        var result = await sut.Handle(cmd, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Message.Should().Be("User not found");
    }

    [Fact]
    public async Task Handle_WhenTryingToLockAdmin_ReturnsFailure()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _adminUserService
            .Setup(x => x.SetUserLockoutAsync(userId, true, null))
            .ReturnsAsync((false, "Cannot lock admin user"));

        var sut = CreateSut();
        var cmd = new LockUserCommand(userId, true, null);

        // Act
        var result = await sut.Handle(cmd, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Message.Should().Be("Cannot lock admin user");
    }
}
