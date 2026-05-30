using Backend.Dtos.Users.Input;

using FluentValidation;

namespace Backend.Validators.Users;

public class UpdateUsernameDtoValidator : AbstractValidator<UpdateUsernameDto>
{
    public UpdateUsernameDtoValidator()
    {
        RuleFor(x => x.Username)
            .MinimumLength(RegisterCredentialsDtoValidator.MinUsernameLength)
                .WithMessage(RegisterCredentialsDtoValidator.InvalidUsernameMessage)
            .MaximumLength(RegisterCredentialsDtoValidator.MaxUsernameLength)
                .WithMessage(RegisterCredentialsDtoValidator.InvalidUsernameMessage)
            .Matches(RegisterCredentialsDtoValidator.UsernameRegex)
                .WithMessage(RegisterCredentialsDtoValidator.InvalidUsernameMessage);
    }
}
