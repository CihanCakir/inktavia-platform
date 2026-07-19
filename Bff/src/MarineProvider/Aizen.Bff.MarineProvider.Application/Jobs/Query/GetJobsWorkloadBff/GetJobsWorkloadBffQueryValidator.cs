using Aizen.Core.Validation;
using FluentValidation;

namespace Aizen.Bff.MarineProvider.Application.Jobs;

public sealed class GetJobsWorkloadBffQueryValidator : AizenValidator<GetJobsWorkloadBffQuery>
{
    public GetJobsWorkloadBffQueryValidator()
    {
        RuleFor(x => x.Weeks).InclusiveBetween(1, 52);
    }
}
