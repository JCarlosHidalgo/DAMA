using Backend.Common;

using FluentValidation;

namespace Backend.Validators;

public class PaginationParamsDtoValidator : AbstractValidator<PaginationParamsDto>
{
    private const int MaxPageIndex = 10000;

    public PaginationParamsDtoValidator()
    {
        RuleFor(x => x.Index)
            .GreaterThanOrEqualTo(0).WithMessage("El índice de página no puede ser negativo.")
            .LessThanOrEqualTo(MaxPageIndex).WithMessage("El índice de página excede el máximo permitido.");
    }
}
