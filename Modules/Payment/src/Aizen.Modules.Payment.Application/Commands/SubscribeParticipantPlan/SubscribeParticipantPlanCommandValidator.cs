using FluentValidation;

namespace Aizen.Modules.Payment.Application.Commands.SubscribeParticipantPlan;

public sealed class SubscribeParticipantPlanCommandValidator
    : AbstractValidator<SubscribeParticipantPlanCommand>
{
    public SubscribeParticipantPlanCommandValidator()
    {
        RuleFor(x => x.ParticipantProfileId)
            .GreaterThan(0).WithMessage("ParticipantProfileId must be greater than zero.");

        RuleFor(x => x.ParticipantPlanId)
            .GreaterThan(0).WithMessage("ParticipantPlanId must be greater than zero.");

        RuleFor(x => x.PaidAmount)
            .GreaterThanOrEqualTo(0).WithMessage("PaidAmount must be zero or greater.");

        RuleFor(x => x.CurrencyCode)
            .NotEmpty().WithMessage("CurrencyCode is required.")
            .Length(3).WithMessage("CurrencyCode must be exactly 3 characters.");

        RuleFor(x => x.PeriodStart)
            .NotEmpty().WithMessage("PeriodStart is required.");

        RuleFor(x => x.PeriodEnd)
            .GreaterThan(x => x.PeriodStart)
            .WithMessage("PeriodEnd must be after PeriodStart.");

        RuleFor(x => x.PaymentTransactionId)
            .GreaterThan(0).When(x => x.PaymentTransactionId.HasValue)
            .WithMessage("PaymentTransactionId must be greater than zero when provided.");
    }
}
