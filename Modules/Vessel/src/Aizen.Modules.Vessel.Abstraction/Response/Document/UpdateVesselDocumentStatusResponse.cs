using Aizen.Modules.Vessel.Abstraction.Enum;

namespace Aizen.Modules.Vessel.Abstraction.Response.Document;

[DocumentationInfo("Update vessel document status response", "Returns the updated document status.")]
public sealed record UpdateVesselDocumentStatusResponse(long VesselId, long DocumentId, VesselDocumentStatus Status);
