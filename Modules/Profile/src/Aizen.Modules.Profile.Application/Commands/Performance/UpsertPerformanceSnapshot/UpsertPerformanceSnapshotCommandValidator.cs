using FluentValidation;

namespace Aizen.Modules.Profile.Application.Commands.Performance.UpsertPerformanceSnapshot;

public sealed class UpsertPerformanceSnapshotCommandValidator
    : AbstractValidator<UpsertPerformanceSnapshotCommand>
{
    public UpsertPerformanceSnapshotCommandValidator()
    {
        RuleFor(x => x.ProfileId)
            .GreaterThan(0).WithMessage("ProfileId must be a positive integer.");

        RuleFor(x => x.ProfileType)
            .IsInEnum().WithMessage("ProfileType must be a valid enum value.");

        RuleFor(x => x.TriggerReason)
            .MaximumLength(200).WithMessage("TriggerReason must not exceed 200 characters.")
            .When(x => x.TriggerReason is not null);
    }
}
