namespace Aizen.Bff.AdminPanel.Application.AdminVessels.Query;

[DocumentationInfo("Get admin vessel media query", "Returns a paged list of media files for the given vessel.")]
public sealed record GetAdminVesselMediaQuery(
    long VesselId,
    string UserToken,
    int PageIndex = 0,
    int PageSize = 50);
