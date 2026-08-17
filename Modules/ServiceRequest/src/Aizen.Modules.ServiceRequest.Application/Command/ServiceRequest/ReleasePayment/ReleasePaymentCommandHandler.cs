using Aizen.Core.CQRS.Handler;
using Aizen.Core.InfoAccessor.Abstraction;
using Aizen.Modules.Payment.Abstraction.RemoteCall;
using Aizen.Modules.Payment.Abstraction.RemoteCall.Requests;
using Aizen.Modules.ServiceRequest.Abstraction.Enum;
using Aizen.Modules.ServiceRequest.Abstraction.Response.ServiceRequest;
using Aizen.Modules.ServiceRequest.Application.Realtime;
using Aizen.Modules.ServiceRequest.Domain.Entities.ServiceRequest;
using Aizen.Modules.ServiceRequest.Domain.Interface.Repository;
using Microsoft.Extensions.Logging;

namespace Aizen.Modules.ServiceRequest.Application.Command.ServiceRequest;

[DocumentationInfo("Release payment command handler", "Releases escrow in Payment module, closes the service request, publishes PaymentReleased event.")]
public sealed class ReleasePaymentCommandHandler : AizenCommandHandler<ReleasePaymentCommand, ReleasePaymentResponse>
{
    private readonly IServiceRequestRepository _repository;
    private readonly IAizenInfoAccessor _info;
    private readonly ServiceRequestRealtimePublisher _realtimePublisher;
    private readonly IPaymentModuleRemoteCall _paymentRemoteCall;
    private readonly ILogger<ReleasePaymentCommandHandler> _logger;

    public ReleasePaymentCommandHandler(
        IServiceRequestRepository repository, IAizenInfoAccessor info,
        ServiceRequestRealtimePublisher realtimePublisher,
        IPaymentModuleRemoteCall paymentRemoteCall,
        ILogger<ReleasePaymentCommandHandler> logger)
    {
        _repository = repository; _info = info;
        _realtimePublisher = realtimePublisher;
        _paymentRemoteCall = paymentRemoteCall;
        _logger = logger;
    }

    public override async Task<ReleasePaymentResponse?> Handle(ReleasePaymentCommand request, CancellationToken cancellationToken)
    {
        var sr = await _repository.GetByIdAsync(request.ServiceRequestId, cancellationToken)
            ?? throw new InvalidOperationException($"ServiceRequest {request.ServiceRequestId} not found.");

        var currentUserId = _info.UserInfoAccessor.UserInfo.UserId;
        var rawToken      = _info.UserInfoAccessor.UserInfo.AccessToken;
        var prevStatus    = sr.Status;

        // ── Release escrow in Payment module (before closing SR) ─────────────
        if (sr.PaymentTransactionId.HasValue)
        {
            try
            {
                var releaseResult = await _paymentRemoteCall.ReleaseEscrowAsync(
                    sr.PaymentTransactionId.Value,
                    new ReleaseEscrowRemoteCallRequest
                    {
                        ApprovedByUserId = currentUserId,
                        AdminNote        = $"SR {sr.RequestCode} completion approved by admin {currentUserId}",
                    },
                    $"Bearer {rawToken}",
                    cancellationToken);

                _logger.LogInformation(
                    "Escrow released for SR {SrId} TxId={TxId}: PayoutRecord={PayoutId} ProviderNet={Net}",
                    sr.Id, sr.PaymentTransactionId.Value,
                    releaseResult.PayoutRecordId, releaseResult.ProviderNetAmount);
            }
            catch (Exception ex)
            {
                // Log and continue — payout can be manually re-triggered via Payment admin panel.
                // Post-MVP: block SR closure until escrow release succeeds.
                _logger.LogError(ex,
                    "Escrow release failed for SR {SrId} TxId={TxId}. SR closed but payout not created.",
                    sr.Id, sr.PaymentTransactionId.Value);
            }
        }
        else
        {
            _logger.LogWarning(
                "SR {SrId} has no PaymentTransactionId — escrow release skipped.", sr.Id);
        }

        sr.ReleasePayment();
        var history = ServiceRequestStatusHistoryEntity.Create(
            sr.Id, prevStatus, ServiceRequestStatus.Closed,
            "Payment released", currentUserId, ServiceRequestActorType.Admin);
        sr.AddStatusHistory(history);
        _repository.Update(sr);

        await _realtimePublisher.PublishAsync(sr.Id, sr.RequestCode, sr.OwnerUserId, sr.Assignment?.ProviderProfileId,
            ServiceRequestRealtimeEventType.PaymentReleased,
            new { ServiceRequestId = sr.Id },
            currentUserId, ServiceRequestActorType.Admin, cancellationToken);

        return new ReleasePaymentResponse(sr.Id);
    }
}
