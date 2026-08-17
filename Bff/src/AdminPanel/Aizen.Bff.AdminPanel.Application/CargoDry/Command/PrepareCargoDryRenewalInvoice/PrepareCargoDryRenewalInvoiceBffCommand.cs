using Aizen.Bff.AdminPanel.Application.CargoDry.Dto;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.CargoDry.Command.PrepareCargoDryRenewalInvoice;

[DocumentationInfo("Prepare CargoDry renewal invoice BFF command",
    "Creates a Draft CargoDryInvoice for the given renewal preparation. " +
    "Does NOT create a PaymentTransaction. Hard rule #16. Idempotent. " +
    "Phase 11 (July 2026).")]
public sealed class PrepareCargoDryRenewalInvoiceBffCommand
    : AizenCommand<PrepareCargoDryRenewalInvoiceBffCommandResponse>
{
    public required long    RenewalPreparationId { get; init; }
    public string?          Note                 { get; init; }
}

public sealed class PrepareCargoDryRenewalInvoiceBffCommandResponse
{
    public CargoDryRenewalPreparationBffDto? Preparation { get; init; }
}
