using Aizen.Core.Validation;
using FluentValidation;

namespace Aizen.Bff.MarineProvider.Application.ServiceRequests;

public sealed class GetServiceRequestDetailBffQueryValidator : AizenValidator<GetServiceRequestDetailBffQuery>
{
    public GetServiceRequestDetailBffQueryValidator()
    {
        RuleFor(x => x.ServiceRequestId).GreaterThan(0);
    }
}
