using Aizen.Core.Validation;
using FluentValidation;

namespace Aizen.Bff.MarineProvider.Application.Jobs;

public sealed class GetJobWorkLogsBffQueryValidator : AizenValidator<GetJobWorkLogsBffQuery>
{
    public GetJobWorkLogsBffQueryValidator()
    {
        RuleFor(x => x.AssignmentId).GreaterThan(0);
    }
}
