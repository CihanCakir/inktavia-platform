using Aizen.Modules.Payment.Abstraction.Model;
using Aizen.Modules.Payment.Abstraction.Request;
using Aizen.Modules.Payment.Abstraction.Response;

namespace Aizen.Modules.Payment.Abstraction.Interface;

/// <summary>
/// Gateway abstraction layer. All payment gateway implementations must implement this interface.
/// Register with keyed DI: services.AddKeyedScoped&lt;IPaymentGatewayProvider, MyProvider&gt;("key").
/// Active gateway is resolved via PaymentGatewayResolver which reads PAYMENT_GATEWAY_ACTIVE system param.
/// </summary>
public interface IPaymentGatewayProvider
{
    /// <summary>Unique gateway identifier — matches PAYMENT_GATEWAY_ACTIVE value.</summary>
    string ProviderKey { get; }

    /// <summary>Initiate a checkout / payment intent. Returns redirect/embed URL and gateway reference.</summary>
    Task<CheckoutInitResult> InitiateCheckoutAsync(CheckoutInitInput input, CancellationToken ct = default);

    /// <summary>
    /// Handle an incoming provider webhook. Must be idempotent —
    /// check TransactionReference already processed before applying.
    /// </summary>
    Task<PaymentApplyResult> HandleWebhookAsync(ProviderWebhookInput input, CancellationToken ct = default);

    /// <summary>Release escrow funds to provider sub-merchant after job completion.</summary>
    Task<PayoutResult> ReleaseEscrowAsync(ReleaseEscrowInput input, CancellationToken ct = default);

    /// <summary>Issue a full or partial refund.</summary>
    Task<RefundResult> RefundAsync(RefundInput input, CancellationToken ct = default);
}
