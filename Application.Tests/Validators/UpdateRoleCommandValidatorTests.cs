using Application.Commands.Role.UpdateRole;
using Application.Validators.Role;
using FluentAssertions;

namespace Application.Tests.Validators;

public class UpdateRoleCommandValidatorTests
{
    private readonly UpdateRoleCommandValidator _validator = new();

    [Fact]
    public void Validate_WhenRoleIdEmpty_ShouldFail()
    {
        // Arrange
        var command = new UpdateRoleCommand(Guid.Empty, "NewName", "Description", new List<string>());

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "RoleId" && e.ErrorMessage.Contains("required"));
    }

    [Fact]
    public void Validate_WhenAllFieldsNull_ShouldPass()
    {
        // Arrange - update with no changes (valid - nothing to validate)
        var command = new UpdateRoleCommand(Guid.NewGuid(), null, null, null);

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WhenNameProvided_ShouldValidateName()
    {
        // Arrange
        var command = new UpdateRoleCommand(Guid.NewGuid(), "A", null, null); // Too short

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Name" && e.ErrorMessage.Contains("2 characters"));
    }

    [Fact]
    public void Validate_WhenNameTooLong_ShouldFail()
    {
        // Arrange
        var longName = new string('A', 51);
        var command = new UpdateRoleCommand(Guid.NewGuid(), longName, null, null);

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Name" && e.ErrorMessage.Contains("50 characters"));
    }

    [Theory]
    [InlineData("1Invalid")]
    [InlineData("_Invalid")]
    [InlineData("Has Space")]
    public void Validate_WhenNameInvalidFormat_ShouldFail(string invalidName)
    {
        // Arrange
        var command = new UpdateRoleCommand(Guid.NewGuid(), invalidName, null, null);

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Name");
    }

    [Fact]
    public void Validate_WhenDescriptionTooLong_ShouldFail()
    {
        // Arrange
        var longDescription = new string('A', 501);
        var command = new UpdateRoleCommand(Guid.NewGuid(), null, longDescription, null);

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Description" && e.ErrorMessage.Contains("500 characters"));
    }

    [Fact]
    public void Validate_WhenDescriptionValidLength_ShouldPass()
    {
        // Arrange
        var description = new string('A', 500);
        var command = new UpdateRoleCommand(Guid.NewGuid(), null, description, null);

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WhenValidNameAndDescription_ShouldPass()
    {
        // Arrange
        var command = new UpdateRoleCommand(Guid.NewGuid(), "NewManager", "New description", new List<string> { "users.read" });

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }
}
