using Aizen.Core.Validation;
using FluentValidation;

namespace Aizen.Bff.MarineProvider.Application.Notifications;

public sealed class SubscribePushCommandValidator : AizenValidator<SubscribePushCommand>
{
    public SubscribePushCommandValidator()
    {
        RuleFor(x => x.Endpoint).NotEmpty();
        RuleFor(x => x.P256dh).NotEmpty();
        RuleFor(x => x.Auth).NotEmpty();
    }
}
