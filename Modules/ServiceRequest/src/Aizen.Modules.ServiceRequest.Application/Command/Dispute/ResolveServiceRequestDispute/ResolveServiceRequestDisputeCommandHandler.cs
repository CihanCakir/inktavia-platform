using System.Text.Json;
using Aizen.Core.Common.Abstraction.Exception;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.InfoAccessor.Abstraction;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Core.Messagebus.Abstraction.Senders;
using Aizen.Modules.Payment.Abstraction.RemoteCall;
using Aizen.Modules.Payment.Abstraction.RemoteCall.Requests;
using Aizen.Modules.Payment.Abstraction.RemoteCall.Responses;
using Aizen.Modules.ServiceRequest.Abstraction.Enum;
using Aizen.Modules.ServiceRequest.Abstraction.Response.Dispute;
using Aizen.Modules.ServiceRequest.Application.Mapping;
using Aizen.Modules.ServiceRequest.Application.Realtime;
using Aizen.Modules.ServiceRequest.Domain.Entities.Dispute;
using Aizen.Modules.ServiceRequest.Domain.Entities.ServiceRequest;
using Aizen.Modules.ServiceRequest.Domain.Interface.Repository;
using Aizen.Modules.ServiceRequest.Repository.Mapping;
using Microsoft.Extensions.Logging;

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
    private readonly ILogger<ResolveServiceRequestDisputeCommandHandler> _logger;

    public ResolveServiceRequestDisputeCommandHandler(
        IServiceRequestRepository srRepository, IServiceRequestDisputeRepository disputeRepository,
        IAizenInfoAccessor info, ServiceRequestRealtimePublisher realtimePublisher,
        IPaymentModuleRemoteCall paymentRemoteCall, IAizenMessagePublisher messagePublisher,
        ILogger<ResolveServiceRequestDisputeCommandHandler> logger)
    {
        _srRepository = srRepository; _disputeRepository = disputeRepository;
        _info = info; _realtimePublisher = realtimePublisher;
        _paymentRemoteCall = paymentRemoteCall; _messagePublisher = messagePublisher;
        _logger = logger;
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
            throw new AizenBusinessException(
                $"Outcome {outcome} requires a positive refund amount.");

        var rawToken = _info.UserInfoAccessor.UserInfo.AccessToken;

        var payload = new ResolveDisputeOutcomeRemoteCallRequest
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
        };

        // The Payment call can fail two ways that must NOT surface as a raw 500:
        //   (a) a downstream business rejection (e.g. amount > refundable) → the module returns a 4xx that Refit
        //       re-throws here as an ApiException — surface its message so the admin sees the real reason;
        //   (b) a transport/availability failure (connection refused, timeout, downstream 5xx) → a generic,
        //       non-leaking business error the admin can retry on.
        // An AizenBusinessException already maps to a clean 400, so we let it through untouched.
        ResolveDisputeOutcomeRemoteCallResponse response;
        try
        {
            response = await _paymentRemoteCall.ResolveDisputeOutcomeAsync(payload, $"Bearer {rawToken}", ct);
        }
        catch (AizenException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "ResolveDispute: payment outcome call failed for dispute {DisputeId} (SR {SRId}, outcome {Outcome}).",
                dispute.Id, sr.Id, outcome);
            throw new AizenBusinessException(DescribePaymentFailure(ex));
        }

        if (response is null)
            throw new AizenBusinessException(
                "The payment service returned no result for the dispute resolution outcome. Please try again.");

        // The admin explicitly chose a monetary outcome that the payment path could not apply — e.g. there is no
        // captured transaction to refund/release. Surface that as a clean business error (using the module's own
        // reason) rather than silently recording a "resolved" dispute with no money moved. `Applied` is true on an
        // idempotent re-resolve (AlreadyApplied), so this only rejects a genuine non-application.
        if (!response.Applied)
            throw new AizenBusinessException(
                string.IsNullOrWhiteSpace(response.Message)
                    ? "The dispute resolution outcome could not be applied to the payment for this service request."
                    : response.Message);

        // Stamp the outcome once — the idempotency anchor. A re-resolve sees IsPaymentOutcomeApplied and never re-drives Payment.
        dispute.MarkPaymentOutcomeApplied(outcome, response.RefundedAmount);
    }

    /// <summary>
    /// Turns a failed Payment remote call into an admin-facing message. Refit surfaces the downstream response body on
    /// an <c>ApiException.Content</c>; we pull the Aizen envelope's <c>header.errorMessage</c> (the real business reason)
    /// via reflection so we don't take a compile-time Refit dependency. Anything else (transport failure, unparseable
    /// body) degrades to a generic, non-leaking message.
    /// </summary>
    private static string DescribePaymentFailure(Exception ex)
    {
        const string generic =
            "The dispute resolution outcome could not be applied by the payment service. Please try again shortly.";

        if (ex.GetType().GetProperty("Content")?.GetValue(ex) is not string content
            || string.IsNullOrWhiteSpace(content))
            return generic;

        try
        {
            using var doc = JsonDocument.Parse(content);
            var root = doc.RootElement;
            if (root.ValueKind == JsonValueKind.Object
                && TryGetPropertyIgnoreCase(root, "header", out var header)
                && TryGetPropertyIgnoreCase(header, "errorMessage", out var msg)
                && msg.ValueKind == JsonValueKind.String
                && !string.IsNullOrWhiteSpace(msg.GetString()))
                return msg.GetString()!;
        }
        catch (JsonException)
        {
            // fall through to the generic message
        }

        return generic;
    }

    private static bool TryGetPropertyIgnoreCase(JsonElement element, string name, out JsonElement value)
    {
        foreach (var prop in element.EnumerateObject())
        {
            if (string.Equals(prop.Name, name, StringComparison.OrdinalIgnoreCase))
            {
                value = prop.Value;
                return true;
            }
        }
        value = default;
        return false;
    }
}
