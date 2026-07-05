using Aizen.Bff.AdminPanel.Application.AdminCargoDry.Dto;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.AdminCargoDry.Command.CompleteCargoDryRenewal;

[DocumentationInfo("Complete CargoDry renewal BFF command",
    "Explicit admin confirmation that completes the renewal and triggers RenewKitCommand (AdminExtension). " +
    "Hard rule #8: must be explicit — renewal is NOT automatic. Phase 11 (July 2026).")]
public sealed class CompleteCargoDryRenewalBffCommand
    : AizenCommand<CompleteCargoDryRenewalBffCommandResponse>
{
    public required long    RenewalPreparationId   { get; init; }
    public required long    CompletedByUserId      { get; init; }
    public string?          ManualPaymentReference { get; init; }
    public string?          Note                   { get; init; }
}

public sealed class CompleteCargoDryRenewalBffCommandResponse
{
    public CargoDryRenewalPreparationBffDto? Preparation { get; init; }
}
