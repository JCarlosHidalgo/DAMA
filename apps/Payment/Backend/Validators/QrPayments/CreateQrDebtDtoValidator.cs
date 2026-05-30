using Backend.Dtos.QrPayments.Input;

using FluentValidation;

namespace Backend.Validators.QrPayments;

public class CreateQrDebtDtoValidator : AbstractValidator<CreateQrDebtDto>
{
    public CreateQrDebtDtoValidator()
    {
        RuleFor(x => x.Email!)
            .EmailAddress().WithMessage("Email con formato inválido.")
            .When(x => !string.IsNullOrEmpty(x.Email));

        RuleFor(x => x.ExternalReference!)
            .MaximumLength(128).WithMessage("ExternalReference máximo 128 caracteres.")
            .When(x => !string.IsNullOrEmpty(x.ExternalReference));
    }
}
