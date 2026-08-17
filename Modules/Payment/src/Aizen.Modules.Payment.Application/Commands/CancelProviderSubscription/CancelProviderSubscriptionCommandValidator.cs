using FluentValidation;

namespace Aizen.Modules.Payment.Application.Commands.CancelProviderSubscription;

public sealed class CancelProviderSubscriptionCommandValidator
    : AbstractValidator<CancelProviderSubscriptionCommand>
{
    public CancelProviderSubscriptionCommandValidator()
    {
        RuleFor(x => x.ProviderProfileId)
            .GreaterThan(0).WithMessage("ProviderProfileId must be greater than zero.");

        RuleFor(x => x.CancellationReason)
            .MaximumLength(500).When(x => x.CancellationReason is not null)
            .WithMessage("CancellationReason must not exceed 500 characters.");
    }
}
