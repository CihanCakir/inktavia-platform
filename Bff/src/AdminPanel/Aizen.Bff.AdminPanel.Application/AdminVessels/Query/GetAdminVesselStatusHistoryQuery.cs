namespace Aizen.Bff.AdminPanel.Application.AdminVessels.Query;

[DocumentationInfo("Get admin vessel status history query", "Returns a paged status change history for the given vessel.")]
public sealed record GetAdminVesselStatusHistoryQuery(
    long VesselId,
    string UserToken,
    int PageIndex = 0,
    int PageSize = 20);
