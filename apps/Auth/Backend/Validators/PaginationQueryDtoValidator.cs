using Backend.Pagination;

using FluentValidation;

namespace Backend.Validators;

public class PaginationQueryDtoValidator : AbstractValidator<PaginationQueryDto>
{
    private const string InvalidPageIndexMessage = "pageIndex no válido";
    private const int MaxPageIndex = 10000;

    public PaginationQueryDtoValidator()
    {
        RuleFor(x => x.PageIndex)
            .GreaterThanOrEqualTo(0).WithMessage(InvalidPageIndexMessage)
            .LessThanOrEqualTo(MaxPageIndex).WithMessage(InvalidPageIndexMessage);
    }
}
