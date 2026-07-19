using Aizen.Core.Validation;
using FluentValidation;

namespace Aizen.Bff.MarineProvider.Application.ServiceRequests;

public sealed class GetProviderDiscoveryBffQueryValidator : AizenValidator<GetProviderDiscoveryBffQuery>
{
    public GetProviderDiscoveryBffQueryValidator()
    {
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
    }
}
