using Application.Commands.User.AssignRoles;
using FluentValidation;

namespace Application.Validators.User;

public class AssignUserRolesCommandValidator : AbstractValidator<AssignUserRolesCommand>
{
    public AssignUserRolesCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("User ID is required");

        RuleFor(x => x.Roles)
            .NotNull().WithMessage("Roles list is required");
    }
}
