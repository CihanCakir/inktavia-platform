using FluentValidation;

namespace Aizen.Modules.Profile.Application.Commands.Performance.ResolveRiskSignal;

public sealed class ResolveRiskSignalCommandValidator
    : AbstractValidator<ResolveRiskSignalCommand>
{
    public ResolveRiskSignalCommandValidator()
    {
        RuleFor(x => x.SignalId)
            .GreaterThan(0).WithMessage("SignalId must be a positive integer.");

        RuleFor(x => x.ResolutionNote)
            .MaximumLength(1000).WithMessage("ResolutionNote must not exceed 1000 characters.")
            .When(x => x.ResolutionNote is not null);
    }
}
