using Application.Commands.User.AssignRoles;
using Application.Validators.User;
using FluentAssertions;

namespace Application.Tests.Validators;

public class AssignUserRolesCommandValidatorTests
{
    private readonly AssignUserRolesCommandValidator _validator = new();

    [Fact]
    public void Validate_WhenValid_ShouldPass()
    {
        // Arrange
        var command = new AssignUserRolesCommand(Guid.NewGuid(), new List<string> { "Admin", "User" });

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WhenUserIdEmpty_ShouldFail()
    {
        // Arrange
        var command = new AssignUserRolesCommand(Guid.Empty, new List<string> { "Admin" });

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "UserId" && e.ErrorMessage.Contains("required"));
    }

    [Fact]
    public void Validate_WhenRolesNull_ShouldFail()
    {
        // Arrange
        var command = new AssignUserRolesCommand(Guid.NewGuid(), null!);

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Roles");
    }

    [Fact]
    public void Validate_WhenRolesEmpty_ShouldPass()
    {
        // Arrange - Empty roles is valid (removes all roles from user)
        var command = new AssignUserRolesCommand(Guid.NewGuid(), new List<string>());

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WhenMultipleRoles_ShouldPass()
    {
        // Arrange
        var command = new AssignUserRolesCommand(
            Guid.NewGuid(), 
            new List<string> { "Admin", "Seller", "ContentManager" }
        );

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }
}
