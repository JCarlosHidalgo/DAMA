using Backend.Dtos.Users.Input;

using FluentValidation;

namespace Backend.Validators.Users;

public class LoginCredentialsDtoValidator : AbstractValidator<LoginCredentialsDto>
{
    public const string InvalidPayloadMessage = "credenciales no válidas";

    public LoginCredentialsDtoValidator()
    {
        RuleFor(x => x.Username)
            .MinimumLength(RegisterCredentialsDtoValidator.MinUsernameLength).WithMessage(InvalidPayloadMessage)
            .MaximumLength(RegisterCredentialsDtoValidator.MaxUsernameLength).WithMessage(InvalidPayloadMessage);

        RuleFor(x => x.Password)
            .MinimumLength(RegisterCredentialsDtoValidator.MinPasswordLength).WithMessage(InvalidPayloadMessage)
            .MaximumLength(RegisterCredentialsDtoValidator.MaxPasswordLength).WithMessage(InvalidPayloadMessage);
    }
}
