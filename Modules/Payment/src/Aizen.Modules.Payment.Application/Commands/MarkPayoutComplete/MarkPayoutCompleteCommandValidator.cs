using FluentValidation;

namespace Aizen.Modules.Payment.Application.Commands.MarkPayoutComplete;

public sealed class MarkPayoutCompleteCommandValidator : AbstractValidator<MarkPayoutCompleteCommand>
{
    public MarkPayoutCompleteCommandValidator()
    {
        RuleFor(x => x.PayoutRecordId)
            .GreaterThan(0).WithMessage("PayoutRecordId must be greater than zero.");

        RuleFor(x => x.GatewayPayoutId)
            .NotEmpty().WithMessage("GatewayPayoutId is required.");
    }
}
