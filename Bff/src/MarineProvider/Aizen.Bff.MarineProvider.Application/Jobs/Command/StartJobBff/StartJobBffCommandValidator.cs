using Aizen.Core.Validation;
using FluentValidation;

namespace Aizen.Bff.MarineProvider.Application.Jobs;

public sealed class StartJobBffCommandValidator : AizenValidator<StartJobBffCommand>
{
    public StartJobBffCommandValidator()
    {
        RuleFor(x => x.AssignmentId).GreaterThan(0);
    }
}
