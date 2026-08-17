using Aizen.Core.CQRS.Message;
using Aizen.Modules.ServiceRequest.Abstraction.Response.ServiceRequest;

namespace Aizen.Modules.ServiceRequest.Application.Query.Provider.GetAttachmentAccessUrl;

public sealed class GetAttachmentAccessCheckQuery : AizenQuery<GetAttachmentAccessCheckResponse>
{
    public long ServiceRequestId { get; }
    public Guid FileId { get; }

    public GetAttachmentAccessCheckQuery(long serviceRequestId, Guid fileId)
    {
        ServiceRequestId = serviceRequestId;
        FileId = fileId;
    }
}
