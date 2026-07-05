using Aizen.Core.CQRS.Message;
using Aizen.Modules.CargoDry.Abstraction.Dto;

namespace Aizen.Modules.CargoDry.Application.Commands.PrepareCargoDryRenewalInvoice;

/// <summary>
/// Prepares a Draft CargoDryInvoice for an existing renewal preparation via Payment module bridge.
/// Idempotent: if InvoiceId already set, returns existing data.
/// Does NOT create PaymentTransaction. Does NOT call Iyzico.
/// Phase 11 (July 2026).
/// </summary>
[DocumentationInfo("Prepare CargoDry renewal invoice command",
    "Creates a Draft CargoDryInvoice in Payment module for an existing renewal preparation. " +
    "Uses ICargoDryRenewalInvoiceService bridge — no direct Payment.Application reference. " +
    "Idempotent. Does NOT create PaymentTransaction. Does NOT call Iyzico. " +
    "Phase 11 (July 2026).")]
public sealed class PrepareCargoDryRenewalInvoiceCommand
    : AizenCommand<CargoDryRenewalPreparationDto>
{
    public required long  RenewalPreparationId { get; init; }
    public string?        Note                 { get; init; }
}
