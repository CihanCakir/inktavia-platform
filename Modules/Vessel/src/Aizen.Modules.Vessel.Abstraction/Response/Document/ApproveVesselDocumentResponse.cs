using Aizen.Modules.Vessel.Abstraction.Dto.Document;

namespace Aizen.Modules.Vessel.Abstraction.Response.Document;

[DocumentationInfo("Approve vessel document response", "Returns the approved vessel document (with ApprovedAt / ApprovedByUserId set).")]
public sealed record ApproveVesselDocumentResponse(VesselDocumentDto Document);
