using Application.Commands.User.AssignRoles;
using Application.DTOs;
using Application.Interfaces;
using FluentAssertions;
using Moq;

namespace Application.Tests.Commands.User;

public class AssignUserRolesCommandHandlerTests
{
    private readonly Mock<IAdminUserService> _adminUserService = new();

    private AssignUserRolesCommandHandler CreateSut() => new(_adminUserService.Object);

    [Fact]
    public async Task Handle_WhenServiceReturnsSuccess_ReturnsSuccessWithUpdatedUser()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var roles = new List<string> { "Admin", "Seller" };
        var updatedUser = new AdminUserDto(
            userId,
            "testuser",
            "Test",
            "User",
            "test@example.com",
            "+380501234567",
            roles,
            null,
            true,
            false,
            null,
            DateTime.UtcNow
        );

        _adminUserService
            .Setup(x => x.AssignUserRolesAsync(userId, roles, It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, "Roles assigned successfully", updatedUser));

        var sut = CreateSut();
        var cmd = new AssignUserRolesCommand(userId, roles);

        // Act
        var result = await sut.Handle(cmd, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Payload.Should().NotBeNull();
        result.Payload!.Roles.Should().BeEquivalentTo(roles);
    }

    [Fact]
    public async Task Handle_WhenUserNotFound_ReturnsFailure()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _adminUserService
            .Setup(x => x.AssignUserRolesAsync(userId, It.IsAny<List<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((false, "User not found", null));

        var sut = CreateSut();
        var cmd = new AssignUserRolesCommand(userId, new List<string> { "Admin" });

        // Act
        var result = await sut.Handle(cmd, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Message.Should().Be("User not found");
        result.Payload.Should().BeNull();
    }

    [Fact]
    public async Task Handle_WhenAssigningEmptyRoles_PassesEmptyList()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var emptyRoles = new List<string>();
        var user = new AdminUserDto(userId, "user", "Name", "Surname", "email@test.com", "", emptyRoles, null, true, false, null, DateTime.UtcNow);

        _adminUserService
            .Setup(x => x.AssignUserRolesAsync(userId, emptyRoles, It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, "Roles cleared", user));

        var sut = CreateSut();
        var cmd = new AssignUserRolesCommand(userId, emptyRoles);

        // Act
        var result = await sut.Handle(cmd, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _adminUserService.Verify(x => x.AssignUserRolesAsync(userId, emptyRoles, It.IsAny<CancellationToken>()), Times.Once);
    }
}
