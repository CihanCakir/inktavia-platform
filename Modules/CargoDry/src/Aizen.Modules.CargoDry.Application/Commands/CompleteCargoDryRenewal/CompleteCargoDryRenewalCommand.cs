using Aizen.Core.CQRS.Message;
using Aizen.Modules.CargoDry.Abstraction.Dto;

namespace Aizen.Modules.CargoDry.Application.Commands.CompleteCargoDryRenewal;

/// <summary>
/// Completes a renewal preparation by calling the existing RenewKitCommand (AdminExtension)
/// and marking the preparation as Completed.
/// Requires explicit admin confirmation — renewal is NOT automatic.
/// Phase 11 (July 2026).
/// </summary>
[DocumentationInfo("Complete CargoDry renewal command",
    "Explicit admin confirmation that completes the renewal preparation and " +
    "calls the existing RenewKitCommand with AdminExtension type. " +
    "Hard rule: renewal must require explicit admin action. " +
    "Phase 11 (July 2026).")]
public sealed class CompleteCargoDryRenewalCommand
    : AizenCommand<CargoDryRenewalPreparationDto>
{
    public required long    RenewalPreparationId   { get; init; }
    /// <summary>Manual payment reference (bank transfer code, receipt number, etc.).</summary>
    public string?          ManualPaymentReference { get; init; }
    public string?          Note                   { get; init; }
}
