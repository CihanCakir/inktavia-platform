using Aizen.Bff.AdminPanel.Application.AdminCargoDry.Dto;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.AdminCargoDry.Command.CancelCargoDryRenewalPreparation;

[DocumentationInfo("Cancel CargoDry renewal preparation BFF command",
    "Cancels an open renewal preparation. CancellationReason is required. " +
    "Phase 11 (July 2026).")]
public sealed class CancelCargoDryRenewalPreparationBffCommand
    : AizenCommand<CancelCargoDryRenewalPreparationBffCommandResponse>
{
    public required long    RenewalPreparationId { get; init; }
    public required long    CancelledByUserId    { get; init; }
    public required string  CancellationReason   { get; init; } = default!;
    public string?          Note                 { get; init; }
}

public sealed class CancelCargoDryRenewalPreparationBffCommandResponse
{
    public CargoDryRenewalPreparationBffDto? Preparation { get; init; }
}
