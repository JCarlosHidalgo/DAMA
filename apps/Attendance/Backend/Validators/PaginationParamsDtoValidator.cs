using Backend.Common;

using FluentValidation;

namespace Backend.Validators;

public class PaginationParamsDtoValidator : AbstractValidator<PaginationParamsDto>
{
    public PaginationParamsDtoValidator()
    {
        RuleFor(pagination => pagination.Index)
            .GreaterThanOrEqualTo(0)
            .WithMessage("El índice de página no puede ser negativo.");
    }
}
