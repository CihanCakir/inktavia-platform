using Aizen.Core.CQRS.Handler;
using Aizen.Core.InfoAccessor.Abstraction;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.Payment.Abstraction.RemoteCall;
using Aizen.Modules.ServiceRequest.Abstraction.Response.Owner;
using Aizen.Modules.ServiceRequest.Domain.Interface.Repository;
using Microsoft.Extensions.Logging;

namespace Aizen.Modules.ServiceRequest.Application.Query.Owner.GetServiceRequestPaymentStatusForOwner;

/// <summary>
/// BE-MO3 — reads the owner-facing payment status of the caller-owner's accepted service request. Owner-scoped
/// like the other owner reads: the caller must own the SR (OwnerUserId == the trusted UserInfo.UserId), else a
/// clean not-found (no existence leak). When the SR has no payment transaction yet (offer not accepted) it returns
/// <c>None</c>. Otherwise it reuses the Payment module's transaction-status read (no capture/webhook logic here)
/// and maps the raw <see cref="PaymentTransactionStatus"/> to a stable owner lifecycle
/// (None / Pending / Paid / Failed / Cancelled). Cost-free: customer total + status + timestamps only.
/// </summary>
public sealed class GetServiceRequestPaymentStatusForOwnerQueryHandler
    : AizenQueryHandler<GetServiceRequestPaymentStatusForOwnerQuery, GetServiceRequestPaymentStatusForOwnerResponse>
{
    private readonly IServiceRequestRepository _srRepository;
    private readonly IPaymentModuleRemoteCall _paymentRemoteCall;
    private readonly IAizenInfoAccessor _info;
    private readonly ILogger<GetServiceRequestPaymentStatusForOwnerQueryHandler> _logger;

    public GetServiceRequestPaymentStatusForOwnerQueryHandler(
        IServiceRequestRepository srRepository,
        IPaymentModuleRemoteCall paymentRemoteCall,
        IAizenInfoAccessor info,
        ILogger<GetServiceRequestPaymentStatusForOwnerQueryHandler> logger)
    {
        _srRepository = srRepository;
        _paymentRemoteCall = paymentRemoteCall;
        _info = info;
        _logger = logger;
    }

    public override async Task<GetServiceRequestPaymentStatusForOwnerResponse?> Handle(
        GetServiceRequestPaymentStatusForOwnerQuery request, CancellationToken ct)
    {
        var ownerUserId = _info.UserInfoAccessor.UserInfo.UserId;

        var sr = await _srRepository.GetByIdAsync(request.ServiceRequestId, ct);
        if (sr is null || sr.OwnerUserId != ownerUserId)
            throw new AizenBusinessException("Service request not found.");

        // No accepted offer yet → no escrow transaction → "None" (not an error; the FE shows the accept CTA).
        if (sr.PaymentTransactionId is not { } txId)
            return new GetServiceRequestPaymentStatusForOwnerResponse
            {
                ServiceRequestId = sr.Id,
                HasPayment       = false,
                Status           = "None",
            };

        var rawToken = _info.UserInfoAccessor.UserInfo.AccessToken;
        var status = await _paymentRemoteCall.GetTransactionStatusAsync(txId, $"Bearer {rawToken}", ct);

        var lifecycle = MapLifecycle((PaymentTransactionStatus)status.StatusCode);

        return new GetServiceRequestPaymentStatusForOwnerResponse
        {
            ServiceRequestId = sr.Id,
            HasPayment       = true,
            TransactionId    = status.TransactionId,
            Status           = lifecycle,
            RawStatus        = status.Status,
            Amount           = status.GrossAmount,
            CurrencyCode     = status.CurrencyCode,
            PaidAt           = status.CapturedAt,
        };
    }

    /// <summary>
    /// Collapse the Payment lifecycle to a stable owner-facing status the FE can switch on. "Paid" = money was
    /// captured (the accept→pay flow succeeded), including later escrow-release / refund / dispute states where a
    /// capture already happened — those are downstream of MO3's accept→pay poll. PendingIntent → still awaiting
    /// capture; Failed/Cancelled → retriable.
    /// </summary>
    private static string MapLifecycle(PaymentTransactionStatus s) => s switch
    {
        PaymentTransactionStatus.PendingIntent => "Pending",
        PaymentTransactionStatus.Failed        => "Failed",
        PaymentTransactionStatus.Cancelled     => "Cancelled",
        _                                       => "Paid",   // Captured / Released / (Partially)Refunded / Disputed — capture happened
    };
}
