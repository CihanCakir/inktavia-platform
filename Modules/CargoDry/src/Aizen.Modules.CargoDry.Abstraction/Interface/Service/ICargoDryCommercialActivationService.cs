namespace Aizen.Modules.CargoDry.Abstraction.Interface.Service;

/// <summary>
/// Resolves the commercial attribution path for a kit immediately after activation.
/// Called by <see cref="ActivateKitCommandHandler"/> after the kit entity is saved.
///
/// Business rules (Phase 3):
/// - ConsignmentSellThrough → creates SalesAttribution (SettlementPending) + locates/creates SellThroughSettlement
/// - ProviderAttributedSale → creates SalesAttribution (Attributed) with commission calculation
/// - DirectSale           → creates SalesAttribution (Attributed), no provider payout
/// - SalesChannel == null → creates SalesAttribution (CommercialReviewRequired), no further action
///
/// Exclusions (Phase 4+):
/// - Does NOT create PaymentTransaction
/// - Does NOT create Invoice
/// - Does NOT generate ProviderPayout record
/// - Does NOT trigger any payment gateway call
/// </summary>
public interface ICargoDryCommercialActivationService
{
    /// <summary>
    /// Resolves and persists the commercial attribution for the activated kit.
    /// All writes are performed within the caller's ambient EF transaction (SaveChangesAsync
    /// is NOT called here — the caller controls the save boundary).
    /// </summary>
    /// <param name="kitId">The activated kit's database Id.</param>
    /// <param name="activatedByUserId">The user who activated the kit.</param>
    /// <param name="ct">Cancellation token.</param>
    Task ResolveAsync(long kitId, long activatedByUserId, CancellationToken ct);
}
