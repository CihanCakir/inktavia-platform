using Aizen.Core.CQRS.Message;
using Aizen.Modules.CargoDry.Abstraction.Dto;

namespace Aizen.Modules.CargoDry.Application.Commands.CancelCargoDryRenewalPreparation;

/// <summary>
/// Cancels an open renewal preparation. Idempotent if already cancelled.
/// Does NOT affect the kit entity or any payment record.
/// Phase 11 (July 2026).
/// </summary>
[DocumentationInfo("Cancel CargoDry renewal preparation command",
    "Cancels an open renewal preparation. Does NOT affect kit or payment entities. " +
    "Idempotent. Phase 11 (July 2026).")]
public sealed class CancelCargoDryRenewalPreparationCommand
    : AizenCommand<CargoDryRenewalPreparationDto>
{
    public required long    RenewalPreparationId  { get; init; }
    public required string  CancellationReason    { get; init; }
    public string?          Note                  { get; init; }
}
