using Backend.Dtos.Tenants.Input;

using FluentValidation;

namespace Backend.Validators.Tenants;

public class UpdateTenantNameDtoValidator : AbstractValidator<UpdateTenantNameDto>
{
    public UpdateTenantNameDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("El nombre del tenant es obligatorio.")
            .MaximumLength(200).WithMessage("El nombre del tenant no puede exceder 200 caracteres.");
    }
}
