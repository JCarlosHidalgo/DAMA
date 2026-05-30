using Backend.Dtos.Schedules.Input;

using FluentValidation;

namespace Backend.Validators.Schedules;

public class CourseScheduleParametersDtoValidator : AbstractValidator<CourseScheduleParametersDto>
{
    private const string InvalidCourseIdMessage = "CourseId es requerido";
    private const string InvalidIndexMessage = "WeekPaginationIndex fuera de rango razonable";

    private const int MinIndex = -520;
    private const int MaxIndex = 520;

    public CourseScheduleParametersDtoValidator()
    {
        RuleFor(x => x.CourseId).NotEqual(Guid.Empty).WithMessage(InvalidCourseIdMessage);

        RuleFor(x => x.WeekPaginationIndex)
            .InclusiveBetween(MinIndex, MaxIndex).WithMessage(InvalidIndexMessage);
    }
}
