using Aizen.Bff.AdminPanel.Application.AdminServiceRequests.Dto;
using Aizen.Core.CQRS.Handler;

namespace Aizen.Bff.AdminPanel.Application.AdminServiceRequests.Query;

[DocumentationInfo("Get admin service request filter options query handler", "Returns static service request status and dispute status option lists for admin filter dropdowns.")]
public sealed class GetAdminServiceRequestFilterOptionsQueryHandler
    : AizenQueryHandler<GetAdminServiceRequestFilterOptionsQuery, AdminServiceRequestFilterOptionsResponse>
{
    public override Task<AdminServiceRequestFilterOptionsResponse?> Handle(
        GetAdminServiceRequestFilterOptionsQuery request, CancellationToken cancellationToken)
    {
        var result = new AdminServiceRequestFilterOptionsResponse
        {
            StatusOptions = new List<string>
            {
                "Pending", "Accepted", "InProgress", "CompletionRequested", "Completed", "Cancelled", "Disputed"
            },
            DisputeStatusOptions = new List<string>
            {
                "Open", "UnderReview", "Resolved", "Rejected", "Closed"
            }
        };

        return Task.FromResult<AdminServiceRequestFilterOptionsResponse?>(result);
    }
}
