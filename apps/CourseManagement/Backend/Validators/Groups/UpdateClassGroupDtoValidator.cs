using Backend.Dtos.Groups.Input;

using FluentValidation;

namespace Backend.Validators.Groups;

public class UpdateClassGroupDtoValidator : AbstractValidator<UpdateClassGroupDto>
{
    public UpdateClassGroupDtoValidator()
    {
        RuleFor(classGroup => classGroup.Name)
            .NotEmpty().WithMessage(CreateClassGroupDtoValidator.InvalidNameMessage)
            .MaximumLength(200).WithMessage(CreateClassGroupDtoValidator.InvalidNameMessage);
    }
}
