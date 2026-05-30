using Backend.Dtos.Courses.Input;

using FluentValidation;

namespace Backend.Validators.Courses;

public class CreateCourseDtoValidator : AbstractValidator<CreateCourseDto>
{
    private const string EmptyNameMessage = "Name es requerido";
    private const string TooLongNameMessage = "Name no debe exceder 100 caracteres";
    private const string TooLongExternalReferenceMessage = "ExternalReference no debe exceder 128 caracteres";

    public CreateCourseDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage(EmptyNameMessage)
            .MaximumLength(100).WithMessage(TooLongNameMessage);

        RuleFor(x => x.ExternalReference)
            .MaximumLength(128).WithMessage(TooLongExternalReferenceMessage)
            .When(x => x.ExternalReference != null);
    }
}
