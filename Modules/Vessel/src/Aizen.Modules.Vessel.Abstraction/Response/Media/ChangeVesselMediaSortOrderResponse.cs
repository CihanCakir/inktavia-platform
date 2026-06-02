using Aizen.Modules.Vessel.Abstraction.Model;

namespace Aizen.Modules.Vessel.Abstraction.Response.Media;

[DocumentationInfo("Change vessel media sort order response", "Returns the updated media sort order.")]
public sealed record ChangeVesselMediaSortOrderResponse(long VesselId, long MediaId, int SortOrder);
