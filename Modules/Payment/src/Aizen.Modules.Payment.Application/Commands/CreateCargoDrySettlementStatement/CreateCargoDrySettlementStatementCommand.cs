using Aizen.Core.CQRS.Message;
using Aizen.Modules.Payment.Abstraction.Model.Result;

namespace Aizen.Modules.Payment.Application.Commands.CreateCargoDrySettlementStatement;

/// <summary>
/// Creates a Draft ProviderSettlementStatement invoice for a CargoDry sell-through monthly settlement.
/// Dispatched in-process by the CargoDry module via ISender.
///
/// Business rules:
///   - InvoiceType = ProviderSettlementStatement (8)
///   - CommercialModel = ConsignmentSettlement (5)
///   - InvoiceSourceType = CargoDrySettlement (8), SourceId = SettlementId
///   - Seller = Inktavia (SellerUserId = null, SellerName = "Inktavia")
///   - Buyer = consignment provider (BuyerUserId = ProviderProfileId)
///   - SubTotal = TaxableAmount = Total = ProviderPayoutAmount (TaxAmount = 0)
///   - Single ProviderSettlementLine at TaxRate = 0, UnitCode = "KIT", Quantity = TotalKitCount
///
/// Idempotent: if a non-cancelled invoice already exists for
///   InvoiceSourceType.CargoDrySettlement + SettlementId, returns the existing record.
///
/// Does NOT issue the invoice. Does NOT create a PaymentTransaction.
/// Phase 4C (July 2026).
/// </summary>
public sealed class CreateCargoDrySettlementStatementCommand
    : AizenCommand<CreateCargoDrySettlementStatementResult>
{
    public required long     SettlementId           { get; init; }
    public required string   SettlementCode         { get; init; }
    public required long     ProviderProfileId      { get; init; }
    public required decimal  ProviderPayoutAmount   { get; init; }
    public required decimal  TotalSaleAmount        { get; init; }
    public required decimal  TotalCommissionAmount  { get; init; }
    public required int      TotalKitCount          { get; init; }
    public required string   CurrencyCode           { get; init; }
    public required string   ProductCode            { get; init; }
    public required DateTime PeriodStartUtc         { get; init; }
    public required DateTime PeriodEndUtc           { get; init; }

    /// <summary>FK to PayoutRecord created in Phase 4B. Stored in ProviderPayoutId.</summary>
    public long? PayoutRecordId { get; init; }

    public required long    PreparedByUserId { get; init; }
    public          string? Notes            { get; init; }
}
