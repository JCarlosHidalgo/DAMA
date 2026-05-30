using Backend.Dtos.Courses.Input;

using FluentValidation;

namespace Backend.Validators.Courses;

public class UpdateCourseDtoValidator : AbstractValidator<UpdateCourseDto>
{
    private const string EmptyNameMessage = "Name es requerido";
    private const string TooLongNameMessage = "Name no debe exceder 100 caracteres";

    public UpdateCourseDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage(EmptyNameMessage)
            .MaximumLength(100).WithMessage(TooLongNameMessage);
    }
}
