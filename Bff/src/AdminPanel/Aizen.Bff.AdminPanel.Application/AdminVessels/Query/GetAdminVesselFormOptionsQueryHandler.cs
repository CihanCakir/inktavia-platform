using Aizen.Bff.AdminPanel.Application.AdminVessels.Dto;
using Aizen.Core.CQRS.Handler;

namespace Aizen.Bff.AdminPanel.Application.AdminVessels.Query;

[DocumentationInfo("Get admin vessel form options query handler", "Returns static vessel type and status option lists for admin vessel management forms.")]
public sealed class GetAdminVesselFormOptionsQueryHandler
    : AizenQueryHandler<GetAdminVesselFormOptionsQuery, AdminVesselFormOptionsResponse>
{
    public override Task<AdminVesselFormOptionsResponse?> Handle(
        GetAdminVesselFormOptionsQuery request, CancellationToken cancellationToken)
    {
        var result = new AdminVesselFormOptionsResponse
        {
            VesselTypes = new List<string>
            {
                "Cargo", "Tanker", "Passenger", "Fishing", "Tug", "Ferry", "Yacht", "Research", "Military", "Other"
            },
            StatusOptions = new List<string>
            {
                "Active", "Inactive", "UnderMaintenance", "Decommissioned", "Archived"
            }
        };

        return Task.FromResult<AdminVesselFormOptionsResponse?>(result);
    }
}
