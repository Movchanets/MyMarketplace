using Application.Commands.Category.UpdateCategory;
using FluentAssertions;

namespace Application.Tests.Validators;

public class UpdateCategoryCommandValidatorTests
{
	private readonly UpdateCategoryCommandValidator _validator = new();

	[Fact]
	public void Validate_WhenIdEmpty_ShouldFail()
	{
		var res = _validator.Validate(new UpdateCategoryCommand(Guid.Empty, "Ok"));
		res.IsValid.Should().BeFalse();
	}

	[Fact]
	public void Validate_WhenNameEmpty_ShouldFail()
	{
		var res = _validator.Validate(new UpdateCategoryCommand(Guid.NewGuid(), ""));
		res.IsValid.Should().BeFalse();
	}

	[Fact]
	public void Validate_WhenDescriptionTooLong_ShouldFail()
	{
		var desc = new string('a', 2001);
		var res = _validator.Validate(new UpdateCategoryCommand(Guid.NewGuid(), "Ok", desc));
		res.IsValid.Should().BeFalse();
	}

	[Fact]
	public void Validate_WhenParentIsEmptyGuid_ShouldFail()
	{
		var res = _validator.Validate(new UpdateCategoryCommand(Guid.NewGuid(), "Ok", null, null, Guid.Empty));
		res.IsValid.Should().BeFalse();
	}
}
