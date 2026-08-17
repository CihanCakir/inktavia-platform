using Aizen.Core.CQRS.Message;
using Aizen.Modules.CargoDry.Abstraction.Dto;

namespace Aizen.Modules.CargoDry.Application.Commands.PrepareCargoDryKitRenewal;

/// <summary>
/// Creates a new CargoDryRenewalPreparationEntity for a kit.
/// Enforces one-open-preparation-per-kit rule.
/// Does NOT create a PaymentTransaction. Does NOT call Iyzico.
/// Phase 11 (July 2026).
/// </summary>
[DocumentationInfo("Prepare CargoDry kit renewal command",
    "Creates a stateful renewal preparation workflow record for admin-driven kit renewal. " +
    "One open preparation per kit is enforced. Renewal completion requires a separate explicit admin action. " +
    "Phase 11 (July 2026).")]
public sealed class PrepareCargoDryKitRenewalCommand
    : AizenCommand<CargoDryRenewalPreparationDto>
{
    public required long    KitId                  { get; init; }
    public required int     RequestedRenewalMonths { get; init; }
    public string?          Note                   { get; init; }
}
