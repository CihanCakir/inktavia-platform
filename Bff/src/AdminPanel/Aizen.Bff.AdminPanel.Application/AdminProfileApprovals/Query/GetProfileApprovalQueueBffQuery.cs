using Aizen.Bff.AdminPanel.Application.AdminProfileApprovals.Dto;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.AdminProfileApprovals.Query;

[DocumentationInfo("Get profile approval queue BFF query", "Fetches combined organizer+venue approval queue with filters and pagination.")]
public sealed class GetProfileApprovalQueueBffQuery : AizenQuery<AdminProfileApprovalQueueBffResponse>
{
    public int PageIndex { get; }
    public int PageSize { get; }
    public string? SearchTerm { get; }
    public string? ProfileType { get; }
    public string? Status { get; }
    public string? SubmittedFrom { get; }
    public string? SubmittedTo { get; }
    public string? RiskLevel { get; }

    public GetProfileApprovalQueueBffQuery(
        int pageIndex,
        int pageSize,
        string? searchTerm,
        string? profileType,
        string? status,
        string? submittedFrom,
        string? submittedTo,
        string? riskLevel)
    {
        PageIndex = pageIndex;
        PageSize = pageSize;
        SearchTerm = searchTerm;
        ProfileType = profileType;
        Status = status;
        SubmittedFrom = submittedFrom;
        SubmittedTo = submittedTo;
        RiskLevel = riskLevel;
    }
}
