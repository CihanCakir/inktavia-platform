using Aizen.Core.RemoteCall.Abstraction;
using Aizen.Modules.CargoDry.Abstraction.RemoteCall.Requests;
using Aizen.Modules.CargoDry.Abstraction.RemoteCall.Responses;

namespace Aizen.Modules.CargoDry.Abstraction.RemoteCall;

/// <summary>
/// Server-to-server (internal) remote-call contract for the CargoDry supply flow, consumed by the ServiceRequest
/// module. Protected by [Authorize] (any valid JWT) — authenticated by the forwarded caller token, not admin-restricted.
/// Lets ServiceRequest gate/pin a CARGODRY_SUPPLY accept and record the supply sale (attribution + preferred provider).
/// </summary>
public interface ICargoDrySupplyRemoteCall : IAizenRemoteCall
{
    /// <summary>
    /// Records the retail sale of an activated supply kit against its source SR — enriches the attribution
    /// (SalePrice + agreement commission → settlement roll-up) + records the preferred provider. Idempotent per SR.
    /// </summary>
    [AizenRemoteCallPost("/api/v1/cargodry/internal/supply/record-sale")]
    Task<RecordCargoDrySupplySaleRemoteResponse> RecordSaleAsync(
        [AizenRemoteCallBody] RecordCargoDrySupplySaleRemoteRequest request,
        [AizenRemoteCallHeader("Authorization")] string authorization,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Reads the accept context: product active/retail state + whether the provider holds an ACTIVE ConsignmentAgreement
    /// (program membership). Used to gate accept and pin the offer price.
    /// </summary>
    [AizenRemoteCallGet("/api/v1/cargodry/internal/supply/accept-context")]
    Task<GetCargoDrySupplyAcceptContextRemoteResponse> GetSupplyAcceptContextAsync(
        long providerProfileId,
        string productCode,
        [AizenRemoteCallHeader("Authorization")] string authorization,
        CancellationToken cancellationToken = default);
}
