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
}
