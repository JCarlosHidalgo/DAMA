using Backend.Dtos.Subscriptions.Input;
using Backend.Entities.Subscriptions;

using FluentValidation;

namespace Backend.Validators.Subscriptions;

public class UpdateSubscriptionPlanDtoValidator : AbstractValidator<UpdateSubscriptionPlanDto>
{
    private const int MaxDays = 365;
    private const int MaxWeeks = 52;
    private const int MaxMonths = 12;

    public UpdateSubscriptionPlanDtoValidator()
    {
        RuleFor(x => x.Price)
            .GreaterThan(0).WithMessage("El precio debe ser mayor a 0.");

        RuleFor(x => x.DurationUnit)
            .Must(unit => Enum.TryParse<SubscriptionDurationUnit>(unit, ignoreCase: true, out _))
            .WithMessage("La unidad de duración debe ser Day, Week o Month.");

        RuleFor(x => x.DurationAmount)
            .GreaterThanOrEqualTo(1).WithMessage("La duración mínima es 1.");

        RuleFor(x => x)
            .Must(HasDurationWithinOneYear)
            .WithMessage("La duración debe estar entre 1 día y 1 año.")
            .When(x => Enum.TryParse<SubscriptionDurationUnit>(x.DurationUnit, ignoreCase: true, out _)
                       && x.DurationAmount >= 1);
    }

    private static bool HasDurationWithinOneYear(UpdateSubscriptionPlanDto dto)
    {
        if (!Enum.TryParse(dto.DurationUnit, ignoreCase: true, out SubscriptionDurationUnit unit))
        {
            return false;
        }

        return unit switch
        {
            SubscriptionDurationUnit.Day => dto.DurationAmount <= MaxDays,
            SubscriptionDurationUnit.Week => dto.DurationAmount <= MaxWeeks,
            SubscriptionDurationUnit.Month => dto.DurationAmount <= MaxMonths,
            _ => false
        };
    }
}
