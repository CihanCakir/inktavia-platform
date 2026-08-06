using Aizen.Core.RemoteCall.Abstraction;
using Aizen.Modules.Payment.Abstraction;
using Aizen.Modules.Payment.Abstraction.RemoteCall.Requests;
using Aizen.Modules.Payment.Abstraction.RemoteCall.Responses;

namespace Aizen.Modules.Payment.Abstraction.RemoteCall;

/// <summary>
/// Server-to-server (internal) remote call contract for the Payment module.
/// Used by ServiceRequest module to create and release escrow transactions.
/// Endpoint is protected by [Authorize] (any valid JWT) — not Admin-restricted,
/// because this is a service-to-service call authenticated by Keycloak.
/// </summary>
[DocumentationInfo("Payment module internal remote call", "Allows other modules (ServiceRequest) to orchestrate escrow lifecycle without going through the admin-only endpoints.")]
public interface IPaymentModuleRemoteCall : IAizenRemoteCall
{
    /// <summary>
    /// Creates a payment escrow after offer acceptance.
    /// Idempotent — duplicate calls with same IdempotencyKey return existing result.
    /// </summary>
    [AizenRemoteCallPost("/api/v1/payment/internal/escrow")]
    Task<CreateEscrowRemoteCallResponse> CreateEscrowAsync(
        [AizenRemoteCallBody] CreateEscrowRemoteCallRequest request,
        [AizenRemoteCallHeader("Authorization")] string authorization,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Releases the escrow held for a specific transaction after SR completion approval.
    /// Creates a PayoutRecord for the provider.
    /// </summary>
    [AizenRemoteCallPost("/api/v1/payment/internal/transactions/{transactionId}/release")]
    Task<ReleaseEscrowRemoteCallResponse> ReleaseEscrowAsync(
        long transactionId,
        [AizenRemoteCallBody] ReleaseEscrowRemoteCallRequest request,
        [AizenRemoteCallHeader("Authorization")] string authorization,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Resolves per-line commissions for an offer's priced lines (BE-S7) via the BE-P2 CommissionRule dimensions.
    /// Pure / read-only / idempotent — no state written, no applied-count bump (that is P8). Used by ServiceRequest
    /// as a compute-on-demand offer-builder preview. Propagates CommissionRuleConflict on a fail-loud tie.
    /// </summary>
    [AizenRemoteCallPost("/api/v1/payment/internal/commission/resolve-lines")]
    Task<ResolveLineCommissionsRemoteCallResponse> ResolveLineCommissionsAsync(
        [AizenRemoteCallBody] ResolveLineCommissionsRemoteCallRequest request,
        [AizenRemoteCallHeader("Authorization")] string authorization,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// BE-P8 — the acceptance-time economics combiner. In ONE idempotent operation: resolves the provider's active plan,
    /// runs the §19.9 order (S7 line commission → P3 platform fee → P5 profit-protection gate → S8 immutable snapshot),
    /// and — only on an approving decision — creates the escrow (gross = CustomerTotal, split = ProviderNet), links
    /// <c>transaction.EconomicsSnapshotId</c>, and bumps applied-count once (P2 MarkApplied). On Rejected/ConfigurationError
    /// no snapshot/escrow is created and the caller must block acceptance. Idempotent on <c>SR-{srId}-OFFER-{offerId}</c>.
    /// </summary>
    [AizenRemoteCallPost("/api/v1/payment/internal/service-request/calculate-economics")]
    Task<CalculateServiceRequestEconomicsRemoteCallResponse> CalculateServiceRequestEconomicsAsync(
        [AizenRemoteCallBody] CalculateServiceRequestEconomicsRemoteCallRequest request,
        [AizenRemoteCallHeader("Authorization")] string authorization,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// BE-I1 — reads whether a provider has a split-eligible sub-merchant (a created/verified sub-merchant with a key).
    /// Pure read. Lets ServiceRequest surface eligibility (e.g. flag a non-payable provider) before the hard P8 gate.
    /// </summary>
    [AizenRemoteCallPost("/api/v1/payment/internal/provider/split-eligibility")]
    Task<GetProviderSplitEligibilityRemoteCallResponse> GetProviderSplitEligibilityAsync(
        [AizenRemoteCallBody] GetProviderSplitEligibilityRemoteCallRequest request,
        [AizenRemoteCallHeader("Authorization")] string authorization,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// BE-S6 — resolves the P6 CustomerDiscountRule for an offer's customer/category/currency and returns the requested
    /// discount + funding split params (flattened). Pure read / compute-on-demand — no persistence, no budget, no snapshot.
    /// The SR offer-builder allocates the requested amount per line + applies the funding split.
    /// </summary>
    [AizenRemoteCallPost("/api/v1/payment/internal/discount/resolve-customer-discount")]
    Task<ResolveCustomerDiscountRemoteCallResponse> ResolveCustomerDiscountAsync(
        [AizenRemoteCallBody] ResolveCustomerDiscountRemoteCallRequest request,
        [AizenRemoteCallHeader("Authorization")] string authorization,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// BE-S5c — resolves the cost-free part-line allowance (max allowed discount + funded split + min-receivable) for an offer's
    /// Product/Consumable lines from the versioned/scoped PartCommercialTerm. Pure read / compute-on-demand — no persistence.
    /// The response carries <b>no</b> supplier cost or dealer margin (cost confidentiality, §20.9). Propagates
    /// PartCommercialTermConflict on a fail-loud tie. Used by ServiceRequest as an offer-builder preview and (later) by S9.
    /// </summary>
    [AizenRemoteCallPost("/api/v1/payment/internal/part-terms/resolve-lines")]
    Task<ResolvePartLineAllowancesRemoteCallResponse> ResolvePartLineAllowancesAsync(
        [AizenRemoteCallBody] ResolvePartLineAllowancesRemoteCallRequest request,
        [AizenRemoteCallHeader("Authorization")] string authorization,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// BE-S13b — drives the P10 refund/escrow path for a resolved dispute (reuses RefundPaymentCommand /
    /// ReleasePaymentEscrowCommand + RefundAllocationService — no bespoke refund math). Idempotent on the dispute
    /// context ref <c>DISPUTE-{DisputeId}</c> so a re-resolve never double-refunds. FavorProviderRelease releases
    /// escrow (no refund); a payer-favoured outcome refunds (validated ≤ refundable).
    /// </summary>
    [AizenRemoteCallPost("/api/v1/payment/internal/service-request/resolve-dispute-outcome")]
    Task<ResolveDisputeOutcomeRemoteCallResponse> ResolveDisputeOutcomeAsync(
        [AizenRemoteCallBody] ResolveDisputeOutcomeRemoteCallRequest request,
        [AizenRemoteCallHeader("Authorization")] string authorization,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// BE-S13a — reads the cost-free P10 payment/refund state + S8 economics for a dispute case file. Pure read;
    /// carries no supplier cost / dealer margin (§20.9).
    /// </summary>
    [AizenRemoteCallPost("/api/v1/payment/internal/service-request/dispute-payment-state")]
    Task<GetDisputeCasePaymentStateRemoteCallResponse> GetDisputeCasePaymentStateAsync(
        [AizenRemoteCallBody] GetDisputeCasePaymentStateRemoteCallRequest request,
        [AizenRemoteCallHeader("Authorization")] string authorization,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// BE-S11b — applies a change-order <b>reduction</b> (removed work) by refunding the delta against the SR's original
    /// escrow via the P10 rails (reuses RefundPaymentCommand primitives / RefundAllocationService — no bespoke refund math,
    /// no mutation of the accepted snapshot). Idempotent on <c>SR-{sr}-OFFER-{offer}-CO-{id}</c> so a re-apply never
    /// double-refunds. A change-order <i>increase</i> uses <see cref="CalculateServiceRequestEconomicsAsync"/> instead
    /// (a new snapshot + incremental escrow under the same CO context ref).
    /// </summary>
    [AizenRemoteCallPost("/api/v1/payment/internal/service-request/apply-change-order-reduction")]
    Task<ApplyChangeOrderReductionRemoteCallResponse> ApplyChangeOrderReductionAsync(
        [AizenRemoteCallBody] ApplyChangeOrderReductionRemoteCallRequest request,
        [AizenRemoteCallHeader("Authorization")] string authorization,
        CancellationToken cancellationToken = default);
}
