using Backend.Dtos.Schedules.Input;

using FluentValidation;

namespace Backend.Validators.Schedules;

public class WeekPointerDtoValidator : AbstractValidator<WeekPointerDto>
{
    private const string InvalidIndexMessage = "WeekPaginationIndex fuera de rango razonable";

    private const int MinIndex = -520;
    private const int MaxIndex = 520;

    public WeekPointerDtoValidator()
    {
        RuleFor(x => x.WeekPaginationIndex)
            .InclusiveBetween(MinIndex, MaxIndex).WithMessage(InvalidIndexMessage);
    }
}
