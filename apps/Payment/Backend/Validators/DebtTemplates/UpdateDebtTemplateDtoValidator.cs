using Backend.Dtos.DebtTemplates.Input;

using FluentValidation;

namespace Backend.Validators.DebtTemplates;

public class UpdateDebtTemplateDtoValidator : AbstractValidator<UpdateDebtTemplateDto>
{
    public UpdateDebtTemplateDtoValidator()
    {
        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Description requerido.")
            .MaximumLength(256).WithMessage("Description máximo 256 caracteres.");

        RuleFor(x => x.ClassQuantity)
            .GreaterThanOrEqualTo(1).WithMessage("ClassQuantity debe ser >= 1.");

        RuleFor(x => x.Cost)
            .GreaterThanOrEqualTo(1).WithMessage("Cost debe ser >= 1.");
    }
}
