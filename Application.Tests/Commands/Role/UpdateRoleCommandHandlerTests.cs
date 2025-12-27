using Application.Commands.Role.UpdateRole;
using Application.DTOs;
using Application.Interfaces;
using FluentAssertions;
using Moq;

namespace Application.Tests.Commands.Role;

public class UpdateRoleCommandHandlerTests
{
    private readonly Mock<IRoleService> _roleService = new();

    private UpdateRoleCommandHandler CreateSut() => new(_roleService.Object);

    [Fact]
    public async Task Handle_WhenServiceReturnsSuccess_ReturnsSuccessWithUpdatedRole()
    {
        // Arrange
        var roleId = Guid.NewGuid();
        var updatedRole = new RoleDto(roleId, "UpdatedRole", "Updated Description", new List<string> { "users.read", "users.write" }, 5);
        
        _roleService
            .Setup(x => x.UpdateRoleAsync(roleId, "UpdatedRole", "Updated Description", It.IsAny<List<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, "Role updated successfully", updatedRole));

        var sut = CreateSut();
        var cmd = new UpdateRoleCommand(roleId, "UpdatedRole", "Updated Description", new List<string> { "users.read", "users.write" });

        // Act
        var result = await sut.Handle(cmd, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Payload.Should().NotBeNull();
        result.Payload!.Name.Should().Be("UpdatedRole");
        result.Payload.Description.Should().Be("Updated Description");
    }

    [Fact]
    public async Task Handle_WhenRoleNotFound_ReturnsFailure()
    {
        // Arrange
        var roleId = Guid.NewGuid();
        _roleService
            .Setup(x => x.UpdateRoleAsync(roleId, It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<List<string>?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((false, "Role not found", null));

        var sut = CreateSut();
        var cmd = new UpdateRoleCommand(roleId, "NewName", null, null);

        // Act
        var result = await sut.Handle(cmd, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Message.Should().Be("Role not found");
    }

    [Fact]
    public async Task Handle_WhenPartialUpdate_PassesNullValues()
    {
        // Arrange
        var roleId = Guid.NewGuid();
        var existingRole = new RoleDto(roleId, "ExistingRole", "Existing Description", new List<string>(), 0);
        
        _roleService
            .Setup(x => x.UpdateRoleAsync(roleId, null, "New Description", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, "Updated", existingRole));

        var sut = CreateSut();
        var cmd = new UpdateRoleCommand(roleId, null, "New Description", null);

        // Act
        await sut.Handle(cmd, CancellationToken.None);

        // Assert
        _roleService.Verify(x => x.UpdateRoleAsync(roleId, null, "New Description", null, It.IsAny<CancellationToken>()), Times.Once);
    }
}
