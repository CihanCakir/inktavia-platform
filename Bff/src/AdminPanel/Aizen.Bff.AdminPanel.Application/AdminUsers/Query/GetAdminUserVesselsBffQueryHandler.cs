using Aizen.Bff.AdminPanel.Application.AdminUsers.Dto;
using Aizen.Core.CQRS.Handler;
using Microsoft.Extensions.Logging;

namespace Aizen.Bff.AdminPanel.Application.AdminUsers.Query;

[DocumentationInfo("Get admin user vessels BFF query handler", "Returns user vessels. TODO: vessel module does not yet expose a userId filter — returns empty list until endpoint is added.")]
public sealed class GetAdminUserVesselsBffQueryHandler
    : AizenQueryHandler<GetAdminUserVesselsBffQuery, AdminUserVesselsBffResponse>
{
    private readonly ILogger<GetAdminUserVesselsBffQueryHandler> _logger;

    public GetAdminUserVesselsBffQueryHandler(ILogger<GetAdminUserVesselsBffQueryHandler> logger)
    {
        _logger = logger;
    }

    public override Task<AdminUserVesselsBffResponse?> Handle(
        GetAdminUserVesselsBffQuery request, CancellationToken cancellationToken)
    {
        // TODO: The Vessel module GetAdminVesselList does not support filtering by userId.
        // A dedicated GET /api/v1/admin/vessels?ownerUserId={id} endpoint needs to be added
        // to the Vessel module and IVesselAdminBffRemoteCall before this can be implemented.
        _logger.LogDebug("[UserVesselsBff] Vessel list by userId not yet supported. Returning empty list.");
        return Task.FromResult<AdminUserVesselsBffResponse?>(new AdminUserVesselsBffResponse());
    }
}
