using Aizen.Core.Validation;
using FluentValidation;

namespace Aizen.Modules.InktaviaStore.Application.Identity.Command.PasswordRecovery.RequestProviderPasswordRecovery;

public sealed class RequestProviderPasswordRecoveryCommandValidator
    : AizenValidator<RequestProviderPasswordRecoveryCommand>
{
    public RequestProviderPasswordRecoveryCommandValidator()
    {
        RuleFor(x => x.Channel).NotEmpty().MaximumLength(16);
        RuleFor(x => x.Identifier).NotEmpty().MaximumLength(256);
    }
}
