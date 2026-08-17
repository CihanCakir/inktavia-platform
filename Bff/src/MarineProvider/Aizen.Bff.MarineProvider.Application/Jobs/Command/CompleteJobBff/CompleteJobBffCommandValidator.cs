using Aizen.Core.Validation;
using FluentValidation;

namespace Aizen.Bff.MarineProvider.Application.Jobs;

public sealed class CompleteJobBffCommandValidator : AizenValidator<CompleteJobBffCommand>
{
    public CompleteJobBffCommandValidator()
    {
        RuleFor(x => x.AssignmentId).GreaterThan(0);
        RuleFor(x => x.Body).NotNull();
    }
}
