using Backend.Dtos.Subscriptions.Input;

using FluentValidation;

namespace Backend.Validators.Subscriptions;

public class CreateSubscriptionDebtDtoValidator : AbstractValidator<CreateSubscriptionDebtDto>
{
    public CreateSubscriptionDebtDtoValidator()
    {
        RuleFor(x => x.Level)
            .InclusiveBetween(1, 3).WithMessage("El nivel de suscripción debe estar entre 1 y 3.");

        RuleFor(x => x.Method)
            .Equal("QR").WithMessage("El único método de pago disponible es QR.");

        RuleFor(x => x.Email!)
            .EmailAddress().WithMessage("Email con formato inválido.")
            .When(x => !string.IsNullOrEmpty(x.Email));
    }
}
