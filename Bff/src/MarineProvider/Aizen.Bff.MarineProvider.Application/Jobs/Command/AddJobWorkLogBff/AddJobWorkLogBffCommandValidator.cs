using Aizen.Core.Validation;
using FluentValidation;

namespace Aizen.Bff.MarineProvider.Application.Jobs;

public sealed class AddJobWorkLogBffCommandValidator : AizenValidator<AddJobWorkLogBffCommand>
{
    public AddJobWorkLogBffCommandValidator()
    {
        RuleFor(x => x.AssignmentId).GreaterThan(0);
        RuleFor(x => x.Body).NotNull();
    }
}
