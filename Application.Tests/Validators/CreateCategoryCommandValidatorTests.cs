using Application.Commands.Category.CreateCategory;
using FluentAssertions;

namespace Application.Tests.Validators;

public class CreateCategoryCommandValidatorTests
{
	private readonly CreateCategoryCommandValidator _validator = new();

	[Fact]
	public void Validate_WhenNameEmpty_ShouldFail()
	{
		var res = _validator.Validate(new CreateCategoryCommand(""));
		res.IsValid.Should().BeFalse();
	}

	[Fact]
	public void Validate_WhenNameTooLong_ShouldFail()
	{
		var name = new string('a', 201);
		var res = _validator.Validate(new CreateCategoryCommand(name));
		res.IsValid.Should().BeFalse();
	}

	[Fact]
	public void Validate_WhenDescriptionTooLong_ShouldFail()
	{
		var desc = new string('a', 2001);
		var res = _validator.Validate(new CreateCategoryCommand("Ok", desc));
		res.IsValid.Should().BeFalse();
	}

	[Fact]
	public void Validate_WhenParentIsEmptyGuid_ShouldFail()
	{
		var res = _validator.Validate(new CreateCategoryCommand("Ok", null, null, Guid.Empty));
		res.IsValid.Should().BeFalse();
	}
}
