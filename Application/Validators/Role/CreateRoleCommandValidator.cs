using Application.Commands.Role.CreateRole;
using FluentValidation;

namespace Application.Validators.Role;

public class CreateRoleCommandValidator : AbstractValidator<CreateRoleCommand>
{
    public CreateRoleCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Role name is required")
            .MinimumLength(2).WithMessage("Role name must be at least 2 characters")
            .MaximumLength(50).WithMessage("Role name cannot exceed 50 characters")
            .Matches("^[a-zA-Z][a-zA-Z0-9_]*$").WithMessage("Role name must start with a letter and contain only letters, numbers, and underscores");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("Description cannot exceed 500 characters");

        RuleFor(x => x.Permissions)
            .NotNull().WithMessage("Permissions list is required");
    }
}
