using Aizen.Bff.AdminPanel.Application.AdminPayment.Dto;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.Infrastructure.Api;
using Aizen.Modules.Identity.Abstraction.Dto.Common;
using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.ServiceRequest.Abstraction.Enum;
using Aizen.Modules.ServiceRequest.Abstraction.Response.ServiceRequest;
using Microsoft.Extensions.Logging;

namespace Aizen.Bff.AdminPanel.Application.AdminPayment.Query.GetPaymentTransactionDetail;

[DocumentationInfo("Get payment transaction detail BFF query handler",
    "Returns a single payment transaction by ID with full cross-module enrichment. " +
    "After fetching the transaction, fires Identity (bulk name resolution) and optionally " +
    "ServiceRequest (code + title + vessel link when ContextType == ServiceRequest) in parallel " +
    "via Task.WhenAll — two parallel round-trips maximum, zero N+1 queries.")]
public sealed class GetPaymentTransactionDetailBffQueryHandler
    : AizenQueryHandler<GetPaymentTransactionDetailBffQuery, GetPaymentTransactionDetailBffResponse>
{
    private readonly IAdminPaymentBffRemoteCall        _payment;
    private readonly IIdentityAdminBffRemoteCall       _identity;
    private readonly IServiceRequestAdminBffRemoteCall _serviceRequest;
    private readonly ILogger<GetPaymentTransactionDetailBffQueryHandler> _logger;

    public GetPaymentTransactionDetailBffQueryHandler(
        IAdminPaymentBffRemoteCall payment,
        IIdentityAdminBffRemoteCall identity,
        IServiceRequestAdminBffRemoteCall serviceRequest,
        ILogger<GetPaymentTransactionDetailBffQueryHandler> logger)
    {
        _payment        = payment;
        _identity       = identity;
        _serviceRequest = serviceRequest;
        _logger         = logger;
    }

    public override async Task<GetPaymentTransactionDetailBffResponse> Handle(
        GetPaymentTransactionDetailBffQuery request, CancellationToken ct)
    {
        // 1. Fetch the transaction — context IDs are not known until this resolves.
        var tx = await _payment.GetTransactionAsync(request.Id, ct);

        if (tx is null)
            return new GetPaymentTransactionDetailBffResponse { Transaction = null };

        // 2. Build profile ID set for Identity call.
        var profileIds = new List<long> { tx.PayerProfileId };
        if (tx.RecipientProfileId.HasValue)
            profileIds.Add(tx.RecipientProfileId.Value);

        // 3. Fire Identity + conditional ServiceRequest calls in parallel.
        //    ServiceRequest call is only needed when ContextType == ServiceRequest.
        var identityTask = _identity.GetUserProfilesByProfileIds(profileIds.ToArray());

        var isServiceRequestContext = tx.ContextType == nameof(TransactionContextType.ServiceRequest);
        var srTask = isServiceRequestContext
            ? _serviceRequest.GetAdminServiceRequestDetail(tx.ContextId)
            : Task.FromResult<AizenApiResponse<GetServiceRequestDetailResponse>?>(null);

        await Task.WhenAll(identityTask, srTask);

        // 4. Resolve Identity names.
        Dictionary<long, UserProfileListItemDto> profileMap = new();
        try
        {
            var identityResult = await identityTask;
            if (identityResult?.Header?.IsSuccess == true && identityResult.Body is { Count: > 0 })
            {
                profileMap = identityResult.Body
                    .GroupBy(p => p.Id)
                    .ToDictionary(g => g.Key, g => g.First());
            }
            else
            {
                _logger.LogWarning(
                    "[PaymentTxDetailBff] Identity call returned IsSuccess={Success} for tx {Id}.",
                    identityResult?.Header?.IsSuccess, request.Id);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[PaymentTxDetailBff] Identity enrichment failed for tx {Id}.", request.Id);
        }

        // 5. Resolve ServiceRequest context (only when ContextType == ServiceRequest).
        string?               srCode     = null;
        string?               srTitle    = null;
        ServiceRequestStatus? srStatus   = null;
        long?                 srVesselId = null;
        long?                 srId       = null;

        if (isServiceRequestContext)
        {
            try
            {
                var srResult = await srTask;
                var sr = srResult?.Body?.Detail?.Request;
                if (sr is not null)
                {
                    srCode     = sr.RequestCode;
                    srTitle    = sr.Title;
                    srStatus   = sr.Status;
                    srVesselId = sr.VesselId;
                    srId       = sr.Id;
                }
                else
                {
                    _logger.LogWarning(
                        "[PaymentTxDetailBff] SR detail returned null for SR id {SrId} (tx {TxId}).",
                        tx.ContextId, request.Id);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "[PaymentTxDetailBff] ServiceRequest enrichment failed for SR id {SrId} (tx {TxId}).",
                    tx.ContextId, request.Id);
            }
        }

        // 6. Merge all enrichment into the final DTO via non-destructive `with` expression.
        var enriched = tx with
        {
            PayerDisplayName       = ResolveDisplayName(profileMap, tx.PayerProfileId),
            RecipientDisplayName   = tx.RecipientProfileId.HasValue
                ? ResolveDisplayName(profileMap, tx.RecipientProfileId.Value)
                : null,
            ServiceRequestCode     = srCode, 
            ServiceRequestTitle    = srTitle,
            ServiceRequestStatus   = srStatus,
            ServiceRequestVesselId = srVesselId,
            ServiceRequestId       = srId,
        };

        return new GetPaymentTransactionDetailBffResponse { Transaction = enriched };
    }

    private static string? ResolveDisplayName(Dictionary<long, UserProfileListItemDto> map, long profileId)
    {
        if (!map.TryGetValue(profileId, out var profile)) return null;
        var name = $"{profile.FirstName} {profile.LastName}".Trim();
        return name.Length > 0 ? name : null;
    }
}
