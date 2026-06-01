using Backend.Dtos.Todotix.Input;

using FluentValidation;

namespace Backend.Validators.Todotix;

public class UpdateTodotixAppKeyDtoValidator : AbstractValidator<UpdateTodotixAppKeyDto>
{
    private const string GuidFormatPattern = "^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$";

    public UpdateTodotixAppKeyDtoValidator()
    {
        RuleFor(x => x.AppKey)
            .NotEmpty().WithMessage("La app-key es requerida.")
            .Matches(GuidFormatPattern).WithMessage("La app-key debe ser un GUID válido (formato 8-4-4-4-12 en minúsculas).");
    }
}
