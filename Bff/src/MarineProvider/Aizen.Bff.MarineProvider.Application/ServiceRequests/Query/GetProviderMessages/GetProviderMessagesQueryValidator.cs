using Aizen.Core.Validation;
using FluentValidation;

namespace Aizen.Bff.MarineProvider.Application.ServiceRequests;

public sealed class GetProviderMessagesQueryValidator : AizenValidator<GetProviderMessagesQuery>
{
    public GetProviderMessagesQueryValidator()
    {
        RuleFor(x => x.ServiceRequestId).GreaterThan(0);
        RuleFor(x => x.Skip).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Take).InclusiveBetween(1, 200);
    }
}
