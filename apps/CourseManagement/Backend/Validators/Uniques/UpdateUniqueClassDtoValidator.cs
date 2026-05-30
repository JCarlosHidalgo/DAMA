using Backend.Dtos.Uniques.Input;
using Backend.Validators.Extensions;
using Backend.Validators.Scheduleds;

using FluentValidation;

namespace Backend.Validators.Uniques;

public class UpdateUniqueClassDtoValidator : AbstractValidator<UpdateUniqueClassDto>
{
    private const string InvalidTimeRangeMessage = "StartTime debe ser menor que EndTime";
    private const string InvalidDateMessage = "Date fuera de rango razonable";

    private static readonly DateOnly MinDate = new DateOnly(2000, 1, 1);
    private static readonly DateOnly MaxDate = new DateOnly(2100, 12, 31);

    public UpdateUniqueClassDtoValidator()
    {
        RuleFor(uniqueClass => uniqueClass.MaxStudentLimit)
            .InclusiveBetween(0, 1000).WithMessage(CreateScheduledClassDtoValidator.InvalidMaxStudentLimitMessage);

        RuleFor(uniqueClass => uniqueClass)
            .Must(uniqueClass => uniqueClass.StartTime < uniqueClass.EndTime).WithMessage(InvalidTimeRangeMessage);

        RuleFor(uniqueClass => uniqueClass)
            .Must(uniqueClass => uniqueClass.StartTime >= CreateScheduledClassDtoValidator.ClassWindowStart
                              && uniqueClass.EndTime <= CreateScheduledClassDtoValidator.ClassWindowEnd)
                .WithMessage(CreateScheduledClassDtoValidator.InvalidTimeWindowMessage);

        RuleFor(uniqueClass => uniqueClass.Date)
            .InclusiveBetween(MinDate, MaxDate).WithMessage(InvalidDateMessage);

        this.MustBeValidTeacherList(uniqueClass => uniqueClass.Teachers);
    }
}
