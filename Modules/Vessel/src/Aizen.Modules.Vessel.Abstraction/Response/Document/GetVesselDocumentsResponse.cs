using Aizen.Modules.Vessel.Abstraction.Dto.Document;
using Aizen.Modules.Vessel.Abstraction.Model;
using MiniUow.Paging;

namespace Aizen.Modules.Vessel.Abstraction.Response.Document;

[DocumentationInfo("Get vessel documents response", "Returns a paged list of vessel documents.")]
public sealed record GetVesselDocumentsResponse(Paginate<VesselDocumentDto> Documents);
