using Aizen.Core.CQRS.Handler;
using Aizen.Core.InfoAccessor.Abstraction;
using Aizen.Modules.Payment.Abstraction.RemoteCall;
using Aizen.Modules.Payment.Abstraction.RemoteCall.Requests;
using Aizen.Modules.Payment.Abstraction.RemoteCall.Responses;
using Aizen.Modules.ServiceRequest.Abstraction.Enum;
using Aizen.Modules.ServiceRequest.Abstraction.RemoteCall;
using Aizen.Modules.ServiceRequest.Abstraction.Response.Dispute;
using Aizen.Modules.ServiceRequest.Application.Mapping;
using Aizen.Modules.ServiceRequest.Domain.Interface.Repository;
using Microsoft.Extensions.Logging;

namespace Aizen.Modules.ServiceRequest.Application.Query.Dispute;

/// <summary>
/// BE-S13a — composes the consolidated dispute case file (§21.5): dispute + SR header + status timeline + N-E
/// structured reason + accepted-offer economics (cost-free Payment S8 snapshot) + work-logs + completion evidence +
/// conversation messages + the P10 payment/refund state. Pure composition via <see cref="DisputeCaseComposer"/>.
/// The Payment read is best-effort: a failure degrades the case view (null payment section) rather than 500-ing.
/// </summary>
[DocumentationInfo("Dispute case detail query handler", "Composes the whole dispute case file for admin adjudication (BE-S13a).")]
public sealed class GetDisputeCaseDetailQueryHandler
    : AizenQueryHandler<GetDisputeCaseDetailQuery, GetDisputeCaseDetailResponse>
{
    private readonly IServiceRequestDisputeRepository _disputeRepository;
    private readonly IServiceRequestRepository        _srRepository;
    private readonly IServiceRequestWorkLogRepository _workLogRepository;
    private readonly IPaymentModuleRemoteCall         _paymentRemoteCall;
    private readonly IServiceRequestMessagingRemoteCall _messagingRemoteCall;
    private readonly IAizenInfoAccessor               _info;
    private readonly ILogger<GetDisputeCaseDetailQueryHandler> _logger;

    public GetDisputeCaseDetailQueryHandler(
        IServiceRequestDisputeRepository disputeRepository,
        IServiceRequestRepository        srRepository,
        IServiceRequestWorkLogRepository workLogRepository,
        IPaymentModuleRemoteCall         paymentRemoteCall,
        IServiceRequestMessagingRemoteCall messagingRemoteCall,
        IAizenInfoAccessor               info,
        ILogger<GetDisputeCaseDetailQueryHandler> logger)
    {
        _disputeRepository = disputeRepository;
        _srRepository      = srRepository;
        _workLogRepository = workLogRepository;
        _paymentRemoteCall = paymentRemoteCall;
        _messagingRemoteCall = messagingRemoteCall;
        _info              = info;
        _logger            = logger;
    }

    public override async Task<GetDisputeCaseDetailResponse?> Handle(
        GetDisputeCaseDetailQuery request, CancellationToken ct)
    {
        var dispute = await _disputeRepository.GetByIdAsync(request.DisputeId, ct)
            ?? throw new InvalidOperationException($"Dispute {request.DisputeId} not found.");

        var sr = await _srRepository.GetByIdWithDetailsAsync(dispute.ServiceRequestId, ct)
            ?? throw new InvalidOperationException($"ServiceRequest {dispute.ServiceRequestId} not found.");

        var providerUserId = sr.Offers
            .FirstOrDefault(o => o.Status == ServiceRequestOfferStatus.Accepted)?.ProviderUserId;

        var workLogs = await _workLogRepository.GetByServiceRequestIdAsync(sr.Id, ct);

        var paymentState = await ReadPaymentStateAsync(sr.Id, ct);

        // BE_WC3b — source the chat transcript from the canonical Messaging store (complete post-cutover). Best-effort:
        // a failure passes null so the composer falls back to sr.Messages (the sync keeps it complete until WC4).
        var transcript = await ReadMessagingTranscriptAsync(sr.Id);

        return DisputeCaseComposer.Compose(dispute, sr, providerUserId, workLogs, paymentState, transcript);
    }

    private async Task<IReadOnlyList<SrTranscriptMessageDto>?> ReadMessagingTranscriptAsync(long srId)
    {
        try
        {
            // contextType = the Messaging MessagingContextType.ServiceRequest member name (the endpoint binds the name).
            var response = await _messagingRemoteCall.GetConversationTranscript("ServiceRequest", srId);
            return response?.Body?.Messages;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "GetDisputeCaseDetail: failed to read Messaging transcript for SR {SRId} — falling back to sr.Messages.", srId);
            return null;
        }
    }

    private async Task<GetDisputeCasePaymentStateRemoteCallResponse?> ReadPaymentStateAsync(long srId, CancellationToken ct)
    {
        try
        {
            var rawToken = _info.UserInfoAccessor.UserInfo.AccessToken;
            return await _paymentRemoteCall.GetDisputeCasePaymentStateAsync(
                new GetDisputeCasePaymentStateRemoteCallRequest { ServiceRequestId = srId },
                $"Bearer {rawToken}", ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "GetDisputeCaseDetail: failed to read Payment state for SR {SRId} — case shown without payment section.", srId);
            return null;
        }
    }
}
