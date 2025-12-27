using Application.Commands.Role.DeleteRole;
using Application.Interfaces;
using FluentAssertions;
using Moq;

namespace Application.Tests.Commands.Role;

public class DeleteRoleCommandHandlerTests
{
    private readonly Mock<IRoleService> _roleService = new();

    private DeleteRoleCommandHandler CreateSut() => new(_roleService.Object);

    [Fact]
    public async Task Handle_WhenServiceReturnsSuccess_ReturnsSuccess()
    {
        // Arrange
        var roleId = Guid.NewGuid();
        _roleService
            .Setup(x => x.DeleteRoleAsync(roleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, "Role deleted successfully"));

        var sut = CreateSut();
        var cmd = new DeleteRoleCommand(roleId);

        // Act
        var result = await sut.Handle(cmd, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Message.Should().Be("Role deleted successfully");
    }

    [Fact]
    public async Task Handle_WhenRoleNotFound_ReturnsFailure()
    {
        // Arrange
        var roleId = Guid.NewGuid();
        _roleService
            .Setup(x => x.DeleteRoleAsync(roleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((false, "Role not found"));

        var sut = CreateSut();
        var cmd = new DeleteRoleCommand(roleId);

        // Act
        var result = await sut.Handle(cmd, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Message.Should().Be("Role not found");
    }

    [Fact]
    public async Task Handle_WhenRoleIsBuiltIn_ReturnsFailure()
    {
        // Arrange
        var roleId = Guid.NewGuid();
        _roleService
            .Setup(x => x.DeleteRoleAsync(roleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((false, "Cannot delete built-in role"));

        var sut = CreateSut();
        var cmd = new DeleteRoleCommand(roleId);

        // Act
        var result = await sut.Handle(cmd, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Message.Should().Be("Cannot delete built-in role");
    }

    [Fact]
    public async Task Handle_PassesCorrectRoleIdToService()
    {
        // Arrange
        var roleId = Guid.NewGuid();
        _roleService
            .Setup(x => x.DeleteRoleAsync(roleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, "Deleted"));

        var sut = CreateSut();
        var cmd = new DeleteRoleCommand(roleId);

        // Act
        await sut.Handle(cmd, CancellationToken.None);

        // Assert
        _roleService.Verify(x => x.DeleteRoleAsync(roleId, It.IsAny<CancellationToken>()), Times.Once);
    }
}
