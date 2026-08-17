using Aizen.Core.Validation;
using FluentValidation;

namespace Aizen.Bff.MarineProvider.Application.ServiceRequests;

public sealed class SendProviderMessageCommandValidator : AizenValidator<SendProviderMessageCommand>
{
    public SendProviderMessageCommandValidator()
    {
        RuleFor(x => x.ServiceRequestId).GreaterThan(0);
    }
}
