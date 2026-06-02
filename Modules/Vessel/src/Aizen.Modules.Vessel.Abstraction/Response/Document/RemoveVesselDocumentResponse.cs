using Aizen.Modules.Vessel.Abstraction.Model;

namespace Aizen.Modules.Vessel.Abstraction.Response.Document;

[DocumentationInfo("Remove vessel document response", "Returns the IDs confirming the document removal.")]
public sealed record RemoveVesselDocumentResponse(long VesselId, long DocumentId);
