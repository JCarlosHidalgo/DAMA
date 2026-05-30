using Backend.Dtos.Users.Input;

using FluentValidation;

namespace Backend.Validators.Users;

public class RegisterCredentialsDtoValidator : AbstractValidator<RegisterCredentialsDto>
{
    public const string InvalidUsernameMessage = "nombre de usuario no válido";
    public const string InvalidPasswordMessage = "contraseña no válida";
    public const int MinUsernameLength = 5;
    public const int MaxUsernameLength = 80;
    public const int MinPasswordLength = 5;
    public const int MaxPasswordLength = 100;
    public const string UsernameRegex = "^[a-zA-Z0-9 ]+$";

    public RegisterCredentialsDtoValidator()
    {
        RuleFor(x => x.Username)
            .MinimumLength(MinUsernameLength).WithMessage(InvalidUsernameMessage)
            .MaximumLength(MaxUsernameLength).WithMessage(InvalidUsernameMessage)
            .Matches(UsernameRegex).WithMessage(InvalidUsernameMessage);

        RuleFor(x => x.Password)
            .MinimumLength(MinPasswordLength).WithMessage(InvalidPasswordMessage)
            .MaximumLength(MaxPasswordLength).WithMessage(InvalidPasswordMessage)
            .Matches("^[a-zA-Z0-9 !@#$%^&*()_+=?-]+$").WithMessage(InvalidPasswordMessage);
    }
}
