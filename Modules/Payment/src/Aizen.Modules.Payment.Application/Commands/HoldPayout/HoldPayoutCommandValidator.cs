using FluentValidation;

namespace Aizen.Modules.Payment.Application.Commands.HoldPayout;

public sealed class HoldPayoutCommandValidator : AbstractValidator<HoldPayoutCommand>
{
    public HoldPayoutCommandValidator()
    {
        RuleFor(x => x.PayoutRecordId)
            .GreaterThan(0).WithMessage("PayoutRecordId must be greater than zero.");

        RuleFor(x => x.HoldReason)
            .NotEmpty().WithMessage("HoldReason is required.")
            .MaximumLength(500).WithMessage("HoldReason must not exceed 500 characters.");

        RuleFor(x => x.AdminNote)
            .MaximumLength(1000).When(x => x.AdminNote is not null)
            .WithMessage("AdminNote must not exceed 1000 characters.");
    }
}
