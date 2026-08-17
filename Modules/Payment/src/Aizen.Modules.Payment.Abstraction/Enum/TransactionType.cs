namespace Aizen.Modules.Payment.Abstraction
// <summary>
// Represents the type of transaction in the Inktavia Store domain.
// </summary>
{
    public enum TransactionType
    {
        // ── Legacy (kept for backward compatibility) ──────────────────────────
        ActivityParticipation = 1,
        VenueReservation      = 2,
        Subscription          = 3,
        FeaturePurchase       = 4,

        // ── Marine OS ─────────────────────────────────────────────────────────
        ServiceRequestEscrow      = 10,  // Escrow hold when SR offer is accepted
        ServiceRequestRefund      = 11,  // Refund on SR cancellation / dispute resolution
        CargoDryRenewal           = 20,  // CargoDry kit renewal payment
        CargoDryRenewalRefund     = 21,  // Refund on CargoDry renewal
        ProviderPlanSubscription  = 30,  // Provider platform subscription (monthly/annual)
        ParticipantSubscription   = 31,  // Participant/customer platform subscription

        // ── Premium (BE-P11) ──────────────────────────────────────────────────
        PremiumBoostPurchase      = 40,  // OFFER_BOOST_7D one-off premium purchase (non-marketplace, whole amount to Inktavia)
    }
}
