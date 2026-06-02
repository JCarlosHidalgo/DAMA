using Backend.Dtos.Users.Input;

using FluentValidation;

namespace Backend.Validators.Users;

public class RefreshTokenRequestDtoValidator : AbstractValidator<RefreshTokenRequestDto>
{
    public const string InvalidPayloadMessage = "token de actualización no válido";

    public RefreshTokenRequestDtoValidator()
    {
        RuleFor(x => x.RefreshToken)
            .NotEmpty().WithMessage(InvalidPayloadMessage);
    }
}
