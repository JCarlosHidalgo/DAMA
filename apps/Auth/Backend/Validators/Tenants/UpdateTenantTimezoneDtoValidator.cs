using Backend.Dtos.Tenants.Input;

using FluentValidation;

namespace Backend.Validators.Tenants;

public class UpdateTenantTimezoneDtoValidator : AbstractValidator<UpdateTenantTimezoneDto>
{
    private const string InvalidTimezoneMessage = "Timezone IANA inválido";

    public UpdateTenantTimezoneDtoValidator()
    {
        RuleFor(x => x.Timezone)
            .NotEmpty().WithMessage(InvalidTimezoneMessage)
            .Must(BeValidIanaTimezone).WithMessage(InvalidTimezoneMessage);
    }

    private static bool BeValidIanaTimezone(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return false;
        }

        try
        {
            TimeZoneInfo.FindSystemTimeZoneById(id);
            return true;
        }
        catch (Exception exception) when (exception is TimeZoneNotFoundException or InvalidTimeZoneException)
        {
            return false;
        }
    }
}
