using Aizen.Modules.Vessel.Abstraction.Model;

namespace Aizen.Modules.Vessel.Abstraction.Response.Vessel;

[DocumentationInfo("Restore vessel response", "Returns the restoration state of the vessel.")]
public sealed record RestoreVesselResponse(long VesselId, bool IsArchived);
