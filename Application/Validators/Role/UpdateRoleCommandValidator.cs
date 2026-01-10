using Application.Commands.Role.UpdateRole;
using FluentValidation;

namespace Application.Validators.Role;

public class UpdateRoleCommandValidator : AbstractValidator<UpdateRoleCommand>
{
    public UpdateRoleCommandValidator()
    {
        RuleFor(x => x.RoleId)
            .NotEmpty().WithMessage("Role ID is required");

        When(x => x.Name != null, () =>
        {
            RuleFor(x => x.Name)
                .MinimumLength(2).WithMessage("Role name must be at least 2 characters")
                .MaximumLength(50).WithMessage("Role name cannot exceed 50 characters")
                .Matches("^[a-zA-Z][a-zA-Z0-9_]*$").WithMessage("Role name must start with a letter and contain only letters, numbers, and underscores");
        });

        When(x => x.Description != null, () =>
        {
            RuleFor(x => x.Description)
                .MaximumLength(500).WithMessage("Description cannot exceed 500 characters");
        });
    }
}
