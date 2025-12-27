using Application.Commands.Role.CreateRole;
using Application.Validators.Role;
using FluentAssertions;

namespace Application.Tests.Validators;

public class CreateRoleCommandValidatorTests
{
    private readonly CreateRoleCommandValidator _validator = new();

    [Fact]
    public void Validate_WhenValid_ShouldPass()
    {
        // Arrange
        var command = new CreateRoleCommand("Manager", "Manager role description", new List<string> { "users.read" });

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WhenNameEmpty_ShouldFail()
    {
        // Arrange
        var command = new CreateRoleCommand("", "Description", new List<string>());

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Name" && e.ErrorMessage.Contains("required"));
    }

    [Fact]
    public void Validate_WhenNameTooShort_ShouldFail()
    {
        // Arrange
        var command = new CreateRoleCommand("A", "Description", new List<string>());

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
        var command = new CreateRoleCommand(longName, "Description", new List<string>());

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Name" && e.ErrorMessage.Contains("50 characters"));
    }

    [Theory]
    [InlineData("1Role")]
    [InlineData("_Role")]
    [InlineData("Role Name")]
    [InlineData("Role-Name")]
    [InlineData("Role.Name")]
    public void Validate_WhenNameInvalidFormat_ShouldFail(string invalidName)
    {
        // Arrange
        var command = new CreateRoleCommand(invalidName, "Description", new List<string>());

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Name" && e.ErrorMessage.Contains("start with a letter"));
    }

    [Theory]
    [InlineData("Admin")]
    [InlineData("Manager")]
    [InlineData("Content_Editor")]
    [InlineData("Role123")]
    [InlineData("My_Role_2024")]
    public void Validate_WhenNameValidFormat_ShouldPass(string validName)
    {
        // Arrange
        var command = new CreateRoleCommand(validName, "Description", new List<string>());

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WhenDescriptionTooLong_ShouldFail()
    {
        // Arrange
        var longDescription = new string('A', 501);
        var command = new CreateRoleCommand("ValidName", longDescription, new List<string>());

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Description" && e.ErrorMessage.Contains("500 characters"));
    }

    [Fact]
    public void Validate_WhenPermissionsNull_ShouldFail()
    {
        // Arrange
        var command = new CreateRoleCommand("ValidName", "Description", null!);

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Permissions");
    }

    [Fact]
    public void Validate_WhenPermissionsEmpty_ShouldPass()
    {
        // Arrange
        var command = new CreateRoleCommand("ValidName", "Description", new List<string>());

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }
}
