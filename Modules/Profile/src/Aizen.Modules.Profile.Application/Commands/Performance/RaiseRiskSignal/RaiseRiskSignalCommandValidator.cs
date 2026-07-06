using FluentValidation;

namespace Aizen.Modules.Profile.Application.Commands.Performance.RaiseRiskSignal;

public sealed class RaiseRiskSignalCommandValidator
    : AbstractValidator<RaiseRiskSignalCommand>
{
    public RaiseRiskSignalCommandValidator()
    {
        RuleFor(x => x.ProfileId)
            .GreaterThan(0).WithMessage("ProfileId must be a positive integer.");

        RuleFor(x => x.ProfileType)
            .IsInEnum().WithMessage("ProfileType must be a valid enum value.");

        RuleFor(x => x.Severity)
            .IsInEnum().WithMessage("Severity must be a valid enum value.");

        RuleFor(x => x.SignalCode)
            .NotEmpty().WithMessage("SignalCode is required.")
            .MaximumLength(100).WithMessage("SignalCode must not exceed 100 characters.");

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Description is required.")
            .MaximumLength(500).WithMessage("Description must not exceed 500 characters.");

        RuleFor(x => x.SourceModule)
            .MaximumLength(100).WithMessage("SourceModule must not exceed 100 characters.")
            .When(x => x.SourceModule is not null);
    }
}
