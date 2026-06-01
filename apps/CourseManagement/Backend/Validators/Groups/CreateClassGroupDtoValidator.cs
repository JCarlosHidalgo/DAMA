using Backend.Dtos.Groups.Input;

using FluentValidation;

namespace Backend.Validators.Groups;

public class CreateClassGroupDtoValidator : AbstractValidator<CreateClassGroupDto>
{
    public const string InvalidNameMessage = "El nombre del grupo es requerido y no debe exceder 200 caracteres";

    public CreateClassGroupDtoValidator()
    {
        RuleFor(classGroup => classGroup.Name)
            .NotEmpty().WithMessage(InvalidNameMessage)
            .MaximumLength(200).WithMessage(InvalidNameMessage);
    }
}
