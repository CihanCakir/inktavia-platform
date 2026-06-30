using Aizen.Modules.Vessel.Abstraction.Enum;

namespace Aizen.Modules.Vessel.Abstraction.Response.Vessel;

[DocumentationInfo("Archive vessel response", "Returns archival details of the vessel.")]
public sealed record ArchiveVesselResponse(long VesselId, bool IsArchived, DateTime? ArchivedAt, VesselArchiveReason? Reason);
