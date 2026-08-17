using Aizen.Modules.Payment.Abstraction.Model.Result;

namespace Aizen.Modules.Payment.Abstraction.Interface;

/// <summary>
/// Service contract allowing the CargoDry module to prepare settlement statement
/// (invoice/document) records for monthly sell-through settlements without taking
/// a compile-time dependency on Payment.Application.
///
/// Implemented in Payment.Application and registered in the shared DI container.
/// CargoDry.Application references Payment.Abstraction and injects this interface.
///
/// The created document is a ProviderSettlementStatement (InvoiceType = 8):
///   - Documents ProviderPayoutAmount owed by Inktavia to the consignment provider.
///   - NOT a buyer invoice. NOT a commission deduction.
///   - Created as Draft only. Finance team issues it manually in Phase 4D.
///   - Tax rate = 0 (inter-party settlement; accountant confirmation required for production).
///
/// Phase 4C (July 2026).
/// </summary>
public interface ICargoDrySettlementInvoiceService
{
    /// <summary>
    /// Creates a Draft ProviderSettlementStatement for a CargoDry sell-through settlement.
    /// Idempotent — if a non-cancelled invoice already exists for InvoiceSourceType.CargoDrySettlement
    /// and the given settlementId, returns the existing record (AlreadyExisted = true).
    ///
    /// Does NOT issue the invoice. Does NOT create a PaymentTransaction.
    /// </summary>
    Task<CreateCargoDrySettlementStatementResult> PrepareSettlementStatementAsync(
        long              settlementId,
        string            settlementCode,
        long              providerProfileId,
        decimal           providerPayoutAmount,
        decimal           totalSaleAmount,
        decimal           totalCommissionAmount,
        int               totalKitCount,
        string            currencyCode,
        string            productCode,
        DateTime          periodStartUtc,
        DateTime          periodEndUtc,
        long?             payoutRecordId,
        long              preparedByUserId,
        string?           notes,
        CancellationToken ct = default);
}
