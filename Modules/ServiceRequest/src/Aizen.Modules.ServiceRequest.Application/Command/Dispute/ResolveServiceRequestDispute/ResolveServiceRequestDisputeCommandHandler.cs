using Aizen.Core.CQRS.Handler;
using Aizen.Core.InfoAccessor.Abstraction;
using Aizen.Core.Messagebus.Abstraction.Senders;
using Aizen.Modules.Payment.Abstraction.RemoteCall;
using Aizen.Modules.Payment.Abstraction.RemoteCall.Requests;
using Aizen.Modules.ServiceRequest.Abstraction.Enum;
using Aizen.Modules.ServiceRequest.Abstraction.Response.Dispute;
using Aizen.Modules.ServiceRequest.Application.Mapping;
using Aizen.Modules.ServiceRequest.Application.Realtime;
using Aizen.Modules.ServiceRequest.Domain.Entities.Dispute;
using Aizen.Modules.ServiceRequest.Domain.Entities.ServiceRequest;
using Aizen.Modules.ServiceRequest.Domain.Interface.Repository;
using Aizen.Modules.ServiceRequest.Repository.Mapping;

namespace Aizen.Modules.ServiceRequest.Application.Command.Dispute;

[DocumentationInfo("Resolve dispute command handler",
    "Admin resolves the dispute, transitions SR to DisputeResolved, drives the optional P10 refund/escrow outcome (BE-S13b), and publishes the DisputeResolved realtime push + bus message (BE-S13c).")]
public sealed class ResolveServiceRequestDisputeCommandHandler : AizenCommandHandler<ResolveServiceRequestDisputeCommand, ResolveServiceRequestDisputeResponse>
{
    private readonly IServiceRequestRepository _srRepository;
    private readonly IServiceRequestDisputeRepository _disputeRepository;
    private readonly IAizenInfoAccessor _info;
    private readonly ServiceRequestRealtimePublisher _realtimePublisher;
    private readonly IPaymentModuleRemoteCall _paymentRemoteCall;
    private readonly IAizenMessagePublisher _messagePublisher;

    public ResolveServiceRequestDisputeCommandHandler(
        IServiceRequestRepository srRepository, IServiceRequestDisputeRepository disputeRepository,
        IAizenInfoAccessor info, ServiceRequestRealtimePublisher realtimePublisher,
        IPaymentModuleRemoteCall paymentRemoteCall, IAizenMessagePublisher messagePublisher)
    {
        _srRepository = srRepository; _disputeRepository = disputeRepository;
        _info = info; _realtimePublisher = realtimePublisher;
        _paymentRemoteCall = paymentRemoteCall; _messagePublisher = messagePublisher;
    }

    public override async Task<ResolveServiceRequestDisputeResponse?> Handle(ResolveServiceRequestDisputeCommand request, CancellationToken cancellationToken)
    {
        var dispute = await _disputeRepository.GetByIdAsync(request.DisputeId, cancellationToken)
            ?? throw new InvalidOperationException($"Dispute {request.DisputeId} not found.");
        var sr = await _srRepository.GetByIdWithDetailsAsync(dispute.ServiceRequestId, cancellationToken)
            ?? throw new InvalidOperationException($"ServiceRequest {dispute.ServiceRequestId} not found.");

        var currentUserId  = _info.UserInfoAccessor.UserInfo.UserId;
        var providerUserId = sr.Offers
            .FirstOrDefault(o => o.Status == ServiceRequestOfferStatus.Accepted)?.ProviderUserId ?? 0;

        // ── Existing behaviour: mark resolved + record the notes (unchanged for a notes-only resolve) ──
        dispute.Resolve(currentUserId, request.Request.ResolutionNotes);

        // ── BE-S13b: opt-in monetary outcome → drive the P10 refund/escrow path (idempotent on DISPUTE-{id}) ──
        var outcome = request.Request.Outcome;
        if (outcome is { } chosen && !dispute.IsPaymentOutcomeApplied)
            await ApplyOutcomeAsync(dispute, sr, chosen, request.Request.RefundAmount, currentUserId, cancellationToken);

        _disputeRepository.Update(dispute);

        var prevStatus = sr.Status;
        sr.ChangeStatus(ServiceRequestStatus.DisputeResolved);
        var history = ServiceRequestStatusHistoryEntity.Create(
            sr.Id, prevStatus, ServiceRequestStatus.DisputeResolved,
            "Dispute resolved", currentUserId, ServiceRequestActorType.Admin);
        sr.AddStatusHistory(history);
        _srRepository.Update(sr);

        await _realtimePublisher.PublishAsync(sr.Id, sr.RequestCode, sr.OwnerUserId, null,
            ServiceRequestRealtimeEventType.DisputeResolved, dispute.ToDto(),
            currentUserId, ServiceRequestActorType.Admin, cancellationToken);

        // ── BE-S13c: resolved lifecycle event for N3 (both parties + outcome). Kept in addition to the realtime push. ──
        await _messagePublisher.PublishAsync(
            DisputeResolvedMessageFactory.Build(dispute, sr.OwnerUserId, providerUserId), cancellationToken);

        return new ResolveServiceRequestDisputeResponse(dispute.Id);
    }

    private async Task ApplyOutcomeAsync(
        ServiceRequestDisputeEntity dispute, ServiceRequestEntity sr, DisputeResolutionOutcome outcome,
        decimal? requestedAmount, long adminUserId, CancellationToken ct)
    {
        // Partial / split require an explicit positive amount (the ≤ refundable check is Payment-side).
        if (DisputeOutcomeRefundMap.RequiresAmount(outcome) && (requestedAmount is null or <= 0m))
            throw new InvalidOperationException(
                $"Outcome {outcome} requires a positive RefundAmount.");

        var rawToken = _info.UserInfoAccessor.UserInfo.AccessToken;

        var response = await _paymentRemoteCall.ResolveDisputeOutcomeAsync(new ResolveDisputeOutcomeRemoteCallRequest
        {
            ServiceRequestId  = sr.Id,
            DisputeId         = dispute.Id,
            OutcomeCode       = (int)outcome,
            ReleaseToProvider = DisputeOutcomeRefundMap.ReleasesEscrow(outcome),
            FullRefund        = DisputeOutcomeRefundMap.IsFullRefund(outcome),
            RefundAmount      = DisputeOutcomeRefundMap.RequiresAmount(outcome) ? requestedAmount : null,
            RefundReasonCode  = (int)DisputeOutcomeRefundMap.ToRefundReason(outcome),
            AdminUserId       = adminUserId,
            Notes             = dispute.ResolutionNotes,
        }, $"Bearer {rawToken}", ct);

        // Stamp the outcome once — the idempotency anchor. A re-resolve sees IsPaymentOutcomeApplied and never re-drives Payment.
        dispute.MarkPaymentOutcomeApplied(outcome, response.RefundedAmount);
    }
}
