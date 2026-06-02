using Aizen.Modules.Vessel.Abstraction.Dto.Document;
using Aizen.Modules.Vessel.Abstraction.Model;

namespace Aizen.Modules.Vessel.Abstraction.Response.Document;

[DocumentationInfo("Add vessel document response", "Returns the newly added vessel document.")]
public sealed record AddVesselDocumentResponse(VesselDocumentDto Document);
