using Aizen.Modules.ServiceRequest.Abstraction.Dto;
using Aizen.Modules.ServiceRequest.Abstraction.Dto.DisputeCase;

namespace Aizen.Modules.ServiceRequest.Abstraction.Response.Dispute;

/// <summary>
/// BE-S13a — the consolidated dispute case file. Pure composition (no logic): the dispute + SR header + status
/// timeline + N-E structured reason + the accepted-offer economics (cost-free S8 snapshot) + work-logs + completion
/// evidence + conversation messages + the P10 payment/refund state. <b>Confidentiality:</b> carries no supplier cost
/// or dealer margin.
/// </summary>
[DocumentationInfo("Dispute case detail response", "One consolidated dispute case file for admin adjudication (BE-S13a).")]
public sealed class GetDisputeCaseDetailResponse
{
    public ServiceRequestDisputeDto Dispute { get; set; } = default!;
    public DisputeCaseServiceRequestDto ServiceRequest { get; set; } = default!;
    public DisputeCaseReasonDto Reason { get; set; } = default!;
    public List<ServiceRequestStatusHistoryDto> StatusTimeline { get; set; } = new();
    public DisputeCaseEconomicsDto? Economics { get; set; }
    public List<DisputeCaseWorkLogDto> WorkLogs { get; set; } = new();
    public DisputeCaseCompletionDto? Completion { get; set; }
    public List<DisputeCaseMessageDto> Messages { get; set; } = new();
    public DisputeCasePaymentStateDto? PaymentState { get; set; }
}
