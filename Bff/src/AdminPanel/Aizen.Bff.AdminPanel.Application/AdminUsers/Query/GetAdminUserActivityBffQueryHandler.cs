using Aizen.Bff.AdminPanel.Application.AdminUsers.Dto;
using Aizen.Core.CQRS.Handler;
using Microsoft.Extensions.Logging;

namespace Aizen.Bff.AdminPanel.Application.AdminUsers.Query;

[DocumentationInfo("Get admin user activity BFF query handler", "Returns activity timeline events. TODO: requires cross-module event aggregation — returns empty list until implemented.")]
public sealed class GetAdminUserActivityBffQueryHandler
    : AizenQueryHandler<GetAdminUserActivityBffQuery, AdminUserActivityBffResponse>
{
    private readonly ILogger<GetAdminUserActivityBffQueryHandler> _logger;

    public GetAdminUserActivityBffQueryHandler(ILogger<GetAdminUserActivityBffQueryHandler> logger)
    {
        _logger = logger;
    }

    public override Task<AdminUserActivityBffResponse?> Handle(
        GetAdminUserActivityBffQuery request, CancellationToken cancellationToken)
    {
        // TODO: Activity aggregation requires:
        // 1. Vessel registrations by userId (vessel module userId filter needed)
        // 2. Transactions by userId (payment module not yet wired)
        // 3. Service requests by userId (SR module userId filter needed)
        // 4. Account status change events (audit log integration needed)
        _logger.LogDebug("[UserActivityBff] Activity aggregation not yet implemented. Returning empty list.");
        return Task.FromResult<AdminUserActivityBffResponse?>(new AdminUserActivityBffResponse());
    }
}
