using Backend.Dtos.Scheduleds.Input;
using Backend.Validators.Extensions;

using FluentValidation;

namespace Backend.Validators.Scheduleds;

public class CreateScheduledClassDtoValidator : AbstractValidator<CreateScheduledClassDto>
{
    public static readonly TimeOnly ClassWindowStart = new TimeOnly(1, 0);
    public static readonly TimeOnly ClassWindowEnd = new TimeOnly(23, 0);
    public const string InvalidTimeWindowMessage = "El horario de clase debe estar entre 01:00 y 23:00 (hora local del tenant)";

    public const string InvalidMaxStudentLimitMessage = "MaxStudentLimit debe estar entre 0 y 1000 (0 = sin límite)";

    private const string InvalidDayOfWeekMessage = "DayOfWeekIndex debe estar entre 1 y 7";
    private const string InvalidTimeRangeMessage = "StartTime debe ser menor que EndTime";
    private const string InvalidCourseIdMessage = "CourseId es requerido";
    private const string TooLongExternalReferenceMessage = "ExternalReference no debe exceder 128 caracteres";

    public CreateScheduledClassDtoValidator()
    {
        RuleFor(scheduledClass => scheduledClass.DayOfWeekIndex)
            .InclusiveBetween(1, 7).WithMessage(InvalidDayOfWeekMessage);

        RuleFor(scheduledClass => scheduledClass.MaxStudentLimit)
            .InclusiveBetween(0, 1000).WithMessage(InvalidMaxStudentLimitMessage);

        RuleFor(scheduledClass => scheduledClass)
            .Must(scheduledClass => scheduledClass.StartTime < scheduledClass.EndTime).WithMessage(InvalidTimeRangeMessage);

        RuleFor(scheduledClass => scheduledClass)
            .Must(scheduledClass => scheduledClass.StartTime >= ClassWindowStart && scheduledClass.EndTime <= ClassWindowEnd)
                .WithMessage(InvalidTimeWindowMessage);

        RuleFor(scheduledClass => scheduledClass.CourseId)
            .NotEqual(Guid.Empty).WithMessage(InvalidCourseIdMessage);

        this.MustBeValidTeacherList(scheduledClass => scheduledClass.Teachers);

        RuleFor(scheduledClass => scheduledClass.ExternalReference)
            .MaximumLength(128).WithMessage(TooLongExternalReferenceMessage)
            .When(scheduledClass => scheduledClass.ExternalReference != null);
    }
}
