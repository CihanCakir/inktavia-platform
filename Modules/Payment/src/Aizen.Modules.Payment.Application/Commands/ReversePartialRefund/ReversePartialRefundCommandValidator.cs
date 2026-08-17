using FluentValidation;

namespace Aizen.Modules.Payment.Application.Commands.ReversePartialRefund;

public sealed class ReversePartialRefundCommandValidator : AbstractValidator<ReversePartialRefundCommand>
{
    public ReversePartialRefundCommandValidator()
    {
        RuleFor(x => x.RefundRecordId)
            .GreaterThan(0).WithMessage("RefundRecordId must be greater than zero.");

        RuleFor(x => x.ReversalReason)
            .NotEmpty().WithMessage("ReversalReason is required.")
            .MaximumLength(500).WithMessage("ReversalReason must not exceed 500 characters.");
    }
}
