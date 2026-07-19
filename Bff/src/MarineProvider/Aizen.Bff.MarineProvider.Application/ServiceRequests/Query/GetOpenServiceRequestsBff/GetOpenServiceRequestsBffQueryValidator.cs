using Aizen.Core.Validation;
using FluentValidation;

namespace Aizen.Bff.MarineProvider.Application.ServiceRequests;

public sealed class GetOpenServiceRequestsBffQueryValidator : AizenValidator<GetOpenServiceRequestsBffQuery>
{
    public GetOpenServiceRequestsBffQueryValidator()
    {
        RuleFor(x => x.PageIndex).GreaterThanOrEqualTo(0);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
    }
}
