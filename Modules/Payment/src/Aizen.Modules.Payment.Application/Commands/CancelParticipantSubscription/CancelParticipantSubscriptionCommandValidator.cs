using FluentValidation;

namespace Aizen.Modules.Payment.Application.Commands.CancelParticipantSubscription;

public sealed class CancelParticipantSubscriptionCommandValidator
    : AbstractValidator<CancelParticipantSubscriptionCommand>
{
    public CancelParticipantSubscriptionCommandValidator()
    {
        RuleFor(x => x.ParticipantProfileId)
            .GreaterThan(0).WithMessage("ParticipantProfileId must be greater than zero.");

        RuleFor(x => x.CancellationReason)
            .MaximumLength(500).When(x => x.CancellationReason is not null)
            .WithMessage("CancellationReason must not exceed 500 characters.");
    }
}
