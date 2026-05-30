using Backend.Dtos.Users.Input;

using FluentValidation;

namespace Backend.Validators.Users;

public class UserSearchQueryDtoValidator : AbstractValidator<UserSearchQueryDto>
{
    public UserSearchQueryDtoValidator()
    {
        RuleFor(x => x.Name)
            .MinimumLength(RegisterCredentialsDtoValidator.MinUsernameLength)
                .WithMessage(RegisterCredentialsDtoValidator.InvalidUsernameMessage)
            .MaximumLength(RegisterCredentialsDtoValidator.MaxUsernameLength)
                .WithMessage(RegisterCredentialsDtoValidator.InvalidUsernameMessage)
            .Matches(RegisterCredentialsDtoValidator.UsernameRegex)
                .WithMessage(RegisterCredentialsDtoValidator.InvalidUsernameMessage);
    }
}
