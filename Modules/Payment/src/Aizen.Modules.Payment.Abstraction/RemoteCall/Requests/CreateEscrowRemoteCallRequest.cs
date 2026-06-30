using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.Payment.Abstraction.Model;

namespace Aizen.Modules.Payment.Abstraction.RemoteCall.Requests;

/// <summary>
/// Request sent from ServiceRequest module → Payment module to create an escrow transaction
/// after a service offer is accepted by the boat owner.
/// </summary>
public sealed class CreateEscrowRemoteCallRequest
{
    /// <summary>
    /// Globally unique key. Format: SR-{serviceRequestId}-OFFER-{offerId}
    /// Prevents duplicate escrow creation on network retry.
    /// </summary>
    public required string IdempotencyKey { get; init; }

    /// <summary>Context binding this transaction to the SR + Offer pair.</summary>
    public required TransactionContext Context { get; init; }

    public required TransactionType TransactionType { get; init; }

    /// <summary>Boat owner's user/profile identifier (payer).</summary>
    public required long PayerProfileId { get; init; }

    /// <summary>
    /// Service provider's profile identifier (recipient).
    /// Null for platform-collected payments (CargoDryRenewal, ProviderSubscription).
    /// </summary>
    public long? RecipientProfileId { get; init; }

    public required decimal GrossAmount { get; init; }
    public decimal DiscountAmount { get; init; } = 0m;
    public required string CurrencyCode { get; init; }

    /// <summary>Provider's active subscription plan — used for commission resolution.</summary>
    public long? ProviderPlanId { get; init; }

    /// <summary>Service category code — used for commission resolution.</summary>
    public string? CategoryCode { get; init; }

    /// <summary>Always true for SR payments (funds held until completion).</summary>
    public bool EscrowRequired { get; init; } = true;
}
