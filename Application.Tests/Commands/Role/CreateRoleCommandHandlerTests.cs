using Application.Commands.Role.CreateRole;
using Application.DTOs;
using Application.Interfaces;
using FluentAssertions;
using Moq;

namespace Application.Tests.Commands.Role;

public class CreateRoleCommandHandlerTests
{
    private readonly Mock<IRoleService> _roleService = new();

    private CreateRoleCommandHandler CreateSut() => new(_roleService.Object);

    [Fact]
    public async Task Handle_WhenServiceReturnsSuccess_ReturnsSuccessWithRole()
    {
        // Arrange
        var roleId = Guid.NewGuid();
        var expectedRole = new RoleDto(roleId, "TestRole", "Test Description", new List<string> { "users.read" }, 0);

        _roleService
            .Setup(x => x.CreateRoleAsync("TestRole", "Test Description", It.IsAny<List<string>>()))
            .ReturnsAsync((true, "Role created successfully", expectedRole));

        var sut = CreateSut();
        var cmd = new CreateRoleCommand("TestRole", "Test Description", new List<string> { "users.read" });

        // Act
        var result = await sut.Handle(cmd, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Payload.Should().NotBeNull();
        result.Payload!.Id.Should().Be(roleId);
        result.Payload.Name.Should().Be("TestRole");
        result.Payload.Permissions.Should().Contain("users.read");
    }

    [Fact]
    public async Task Handle_WhenServiceReturnsFailure_ReturnsFailure()
    {
        // Arrange
        _roleService
            .Setup(x => x.CreateRoleAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>()))
            .ReturnsAsync((false, "Role already exists", null));

        var sut = CreateSut();
        var cmd = new CreateRoleCommand("Admin", "Admin role", new List<string>());

        // Act
        var result = await sut.Handle(cmd, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Message.Should().Be("Role already exists");
        result.Payload.Should().BeNull();
    }

    [Fact]
    public async Task Handle_PassesCorrectParametersToService()
    {
        // Arrange
        var permissions = new List<string> { "users.read", "users.write" };
        _roleService
            .Setup(x => x.CreateRoleAsync("Manager", "Manager role", permissions))
            .ReturnsAsync((true, "Created", new RoleDto(Guid.NewGuid(), "Manager", "Manager role", permissions, 0)));

        var sut = CreateSut();
        var cmd = new CreateRoleCommand("Manager", "Manager role", permissions);

        // Act
        await sut.Handle(cmd, CancellationToken.None);

        // Assert
        _roleService.Verify(x => x.CreateRoleAsync("Manager", "Manager role", permissions), Times.Once);
    }
}
