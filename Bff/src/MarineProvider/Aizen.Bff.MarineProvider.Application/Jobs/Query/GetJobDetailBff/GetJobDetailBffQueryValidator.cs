using Aizen.Core.Validation;
using FluentValidation;

namespace Aizen.Bff.MarineProvider.Application.Jobs;

public sealed class GetJobDetailBffQueryValidator : AizenValidator<GetJobDetailBffQuery>
{
    public GetJobDetailBffQueryValidator()
    {
        RuleFor(x => x.AssignmentId).GreaterThan(0);
    }
}
