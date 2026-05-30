using Backend.Dtos.Remain.Input;
using Backend.Options;

using FluentValidation;

using Microsoft.Extensions.Options;

namespace Backend.Validators.Remain;

public class IncrementStudentRemainDtoValidator : AbstractValidator<IncrementStudentRemainDto>
{
    public IncrementStudentRemainDtoValidator(IOptions<RemainLimits> remainLimitsOptions)
    {
        RemainLimits remainLimits = remainLimitsOptions.Value;
        string invalidQuantityMessage =
            $"Quantity debe ser un entero entre {remainLimits.MinIncrement} y {remainLimits.MaxIncrement}";
        string invalidStudentNameMessage =
            $"StudentName no debe exceder {remainLimits.MaxStudentNameLength} caracteres";

        RuleFor(incrementRequest => incrementRequest.RequestId)
            .NotEqual(Guid.Empty)
            .WithMessage("RequestId es obligatorio para garantizar idempotencia");

        RuleFor(incrementRequest => incrementRequest.Quantity)
            .InclusiveBetween(remainLimits.MinIncrement, remainLimits.MaxIncrement)
            .WithMessage(invalidQuantityMessage);

        RuleFor(incrementRequest => incrementRequest.StudentName!)
            .MaximumLength(remainLimits.MaxStudentNameLength)
            .WithMessage(invalidStudentNameMessage)
            .When(incrementRequest => !string.IsNullOrEmpty(incrementRequest.StudentName));
    }
}
