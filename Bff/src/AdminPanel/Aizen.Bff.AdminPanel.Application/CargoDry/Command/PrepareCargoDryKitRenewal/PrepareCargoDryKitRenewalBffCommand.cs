using Aizen.Bff.AdminPanel.Application.CargoDry.Dto;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.CargoDry.Command.PrepareCargoDryKitRenewal;

[DocumentationInfo("Prepare CargoDry kit renewal BFF command",
    "Creates a new renewal preparation workflow record for a kit. " +
    "One open preparation per kit is enforced in the module layer. " +
    "Phase 11 (July 2026).")]
public sealed class PrepareCargoDryKitRenewalBffCommand
    : AizenCommand<PrepareCargoDryKitRenewalBffCommandResponse>
{
    public required long    KitId                  { get; init; }
    public required int     RequestedRenewalMonths { get; init; }
    public required long    PreparedByUserId       { get; init; }
    public string?          Note                   { get; init; }
}

public sealed class PrepareCargoDryKitRenewalBffCommandResponse
{
    public CargoDryRenewalPreparationBffDto? Preparation { get; init; }
}
