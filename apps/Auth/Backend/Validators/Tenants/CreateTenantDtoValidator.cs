using Backend.Dtos.Tenants.Input;

using FluentValidation;

namespace Backend.Validators.Tenants;

public class CreateTenantDtoValidator : AbstractValidator<CreateTenantDto>
{
    public CreateTenantDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("El nombre del tenant es obligatorio.")
            .MaximumLength(200).WithMessage("El nombre del tenant no puede exceder 200 caracteres.");
    }
}
