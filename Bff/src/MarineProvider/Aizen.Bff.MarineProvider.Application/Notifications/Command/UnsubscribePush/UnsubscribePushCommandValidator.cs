using Aizen.Core.Validation;
using FluentValidation;

namespace Aizen.Bff.MarineProvider.Application.Notifications;

public sealed class UnsubscribePushCommandValidator : AizenValidator<UnsubscribePushCommand>
{
    public UnsubscribePushCommandValidator()
    {
        RuleFor(x => x.Endpoint).NotEmpty();
    }
}
