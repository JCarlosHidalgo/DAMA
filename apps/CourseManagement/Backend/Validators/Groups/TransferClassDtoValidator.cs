using Backend.Dtos.Groups.Input;

using FluentValidation;

namespace Backend.Validators.Groups;

public class TransferClassDtoValidator : AbstractValidator<TransferClassDto>
{
    private const string InvalidTargetGroupIdMessage = "TargetGroupId es requerido";

    public TransferClassDtoValidator()
    {
        RuleFor(transfer => transfer.TargetGroupId)
            .NotEqual(Guid.Empty).WithMessage(InvalidTargetGroupIdMessage);
    }
}
