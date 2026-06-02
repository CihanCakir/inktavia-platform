using Aizen.Core.CQRS.Message;
using Aizen.Modules.Vessel.Abstraction.Dto.Document;

namespace Aizen.Modules.Vessel.Application.Query.Document;

public sealed class GetVesselDocumentsQuery : AizenQuery<IReadOnlyList<VesselDocumentDto>>
{
    public long VesselId { get; }

    public GetVesselDocumentsQuery(long vesselId)
    {
        VesselId = vesselId;
    }
}
