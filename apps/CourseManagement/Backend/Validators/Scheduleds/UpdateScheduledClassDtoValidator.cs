using Backend.Dtos.Scheduleds.Input;
using Backend.Validators.Extensions;

using FluentValidation;

namespace Backend.Validators.Scheduleds;

public class UpdateScheduledClassDtoValidator : AbstractValidator<UpdateScheduledClassDto>
{
    private const string InvalidDayOfWeekMessage = "DayOfWeekIndex debe estar entre 1 y 7";
    private const string InvalidTimeRangeMessage = "StartTime debe ser menor que EndTime";

    public UpdateScheduledClassDtoValidator()
    {
        RuleFor(scheduledClass => scheduledClass.DayOfWeekIndex)
            .InclusiveBetween(1, 7).WithMessage(InvalidDayOfWeekMessage);

        RuleFor(scheduledClass => scheduledClass.MaxStudentLimit)
            .InclusiveBetween(0, 1000).WithMessage(CreateScheduledClassDtoValidator.InvalidMaxStudentLimitMessage);

        RuleFor(scheduledClass => scheduledClass)
            .Must(scheduledClass => scheduledClass.StartTime < scheduledClass.EndTime).WithMessage(InvalidTimeRangeMessage);

        RuleFor(scheduledClass => scheduledClass)
            .Must(scheduledClass => scheduledClass.StartTime >= CreateScheduledClassDtoValidator.ClassWindowStart
                                 && scheduledClass.EndTime <= CreateScheduledClassDtoValidator.ClassWindowEnd)
                .WithMessage(CreateScheduledClassDtoValidator.InvalidTimeWindowMessage);

        this.MustBeValidTeacherList(scheduledClass => scheduledClass.Teachers);
    }
}
