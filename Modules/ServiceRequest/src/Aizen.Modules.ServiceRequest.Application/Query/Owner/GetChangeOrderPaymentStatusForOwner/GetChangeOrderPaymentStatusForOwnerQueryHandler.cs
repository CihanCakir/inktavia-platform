using Aizen.Core.CQRS.Handler;
using Aizen.Core.InfoAccessor.Abstraction;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.Payment.Abstraction.RemoteCall;
using Aizen.Modules.ServiceRequest.Abstraction.Response.Owner;
using Aizen.Modules.ServiceRequest.Domain.Interface.Repository;
using Microsoft.Extensions.Logging;

namespace Aizen.Modules.ServiceRequest.Application.Query.Owner.GetChangeOrderPaymentStatusForOwner;

/// <summary>
/// BE-MO6 — reads the owner-facing payment status of a change order's incremental escrow transaction. Owner-scoped
/// like the sibling MO3 SR payment-status: the caller must own the SR (OwnerUserId == the trusted UserInfo.UserId),
/// else a clean not-found. The change order must belong to that SR. When the CO has no transaction yet (still
/// Proposed, or a Decrease with no incremental escrow) it returns <c>None</c>. Otherwise it reuses the Payment
/// transaction-status read (no capture/webhook logic here) and maps the raw status to the same stable owner lifecycle
/// (None / Pending / Paid / Failed / Cancelled) the MO3 poll uses. Cost-free: customer total + status + timestamps.
/// </summary>
public sealed class GetChangeOrderPaymentStatusForOwnerQueryHandler
    : AizenQueryHandler<GetChangeOrderPaymentStatusForOwnerQuery, GetServiceRequestPaymentStatusForOwnerResponse>
{
    private readonly IServiceRequestRepository _srRepository;
    private readonly IServiceChangeOrderRepository _changeOrderRepository;
    private readonly IPaymentModuleRemoteCall _paymentRemoteCall;
    private readonly IAizenInfoAccessor _info;
    private readonly ILogger<GetChangeOrderPaymentStatusForOwnerQueryHandler> _logger;

    public GetChangeOrderPaymentStatusForOwnerQueryHandler(
        IServiceRequestRepository srRepository,
        IServiceChangeOrderRepository changeOrderRepository,
        IPaymentModuleRemoteCall paymentRemoteCall,
        IAizenInfoAccessor info,
        ILogger<GetChangeOrderPaymentStatusForOwnerQueryHandler> logger)
    {
        _srRepository = srRepository;
        _changeOrderRepository = changeOrderRepository;
        _paymentRemoteCall = paymentRemoteCall;
        _info = info;
        _logger = logger;
    }

    public override async Task<GetServiceRequestPaymentStatusForOwnerResponse?> Handle(
        GetChangeOrderPaymentStatusForOwnerQuery request, CancellationToken ct)
    {
        var ownerUserId = _info.UserInfoAccessor.UserInfo.UserId;

        var sr = await _srRepository.GetByIdAsync(request.ServiceRequestId, ct);
        if (sr is null || sr.OwnerUserId != ownerUserId)
            throw new AizenBusinessException("Change order not found.");

        var co = await _changeOrderRepository.GetByIdAsync(request.ChangeOrderId, ct);
        if (co is null || co.ServiceRequestId != request.ServiceRequestId)
            throw new AizenBusinessException("Change order not found.");

        // No incremental escrow transaction yet (still Proposed, or a Decrease that only refunds) → "None".
        if (co.PaymentTransactionId is not { } txId || txId <= 0)
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

    // Mirrors GetServiceRequestPaymentStatusForOwnerQueryHandler.MapLifecycle (kept identical so the incremental poll
    // reports the same owner lifecycle as the acceptance poll). "Paid" = a capture happened.
    private static string MapLifecycle(PaymentTransactionStatus s) => s switch
    {
        PaymentTransactionStatus.PendingIntent => "Pending",
        PaymentTransactionStatus.Failed        => "Failed",
        PaymentTransactionStatus.Cancelled     => "Cancelled",
        _                                       => "Paid",
    };
}
