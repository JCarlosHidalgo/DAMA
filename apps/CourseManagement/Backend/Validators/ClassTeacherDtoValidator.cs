using Backend.Dtos;

using FluentValidation;

namespace Backend.Validators;

public class ClassTeacherDtoValidator : AbstractValidator<ClassTeacherDto>
{
    private const string InvalidTeacherIdMessage = "TeacherId es requerido";
    private const string EmptyTeacherNameMessage = "TeacherName es requerido";
    private const string TooLongTeacherNameMessage = "TeacherName no debe exceder 200 caracteres";

    public ClassTeacherDtoValidator()
    {
        RuleFor(x => x.TeacherId)
            .NotEqual(Guid.Empty).WithMessage(InvalidTeacherIdMessage);

        RuleFor(x => x.TeacherName)
            .NotEmpty().WithMessage(EmptyTeacherNameMessage)
            .MaximumLength(200).WithMessage(TooLongTeacherNameMessage);
    }
}
