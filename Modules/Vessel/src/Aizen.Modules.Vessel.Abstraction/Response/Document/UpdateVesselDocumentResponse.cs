using Aizen.Modules.Vessel.Abstraction.Dto.Document;

namespace Aizen.Modules.Vessel.Abstraction.Response.Document;

[DocumentationInfo("Update vessel document response", "Returns the updated vessel document.")]
public sealed record UpdateVesselDocumentResponse(VesselDocumentDto Document);
