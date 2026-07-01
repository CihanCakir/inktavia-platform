using FluentValidation;

namespace Aizen.Modules.Payment.Application.Commands.ApproveManualPayout;

public sealed class ApproveManualPayoutCommandValidator : AbstractValidator<ApproveManualPayoutCommand>
{
    public ApproveManualPayoutCommandValidator()
    {
        RuleFor(x => x.PayoutRecordId)
            .GreaterThan(0).WithMessage("PayoutRecordId must be greater than zero.");

        RuleFor(x => x.GatewayPayoutId)
            .NotEmpty().WithMessage("GatewayPayoutId is required.")
            .MaximumLength(200).WithMessage("GatewayPayoutId must not exceed 200 characters.");

        RuleFor(x => x.AdminNote)
            .MaximumLength(1000).When(x => x.AdminNote is not null)
            .WithMessage("AdminNote must not exceed 1000 characters.");
    }
}
