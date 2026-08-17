using Aizen.Core.CQRS.Message;
using Aizen.Modules.ServiceRequest.Abstraction.Response.ServiceRequest;

namespace Aizen.Modules.ServiceRequest.Application.Query.Owner.GetOwnerAttachmentAccessCheck;

/// <summary>
/// BE-MO10a/b — access-checks a specific attachment on a specific service request for the calling OWNER. Mirrors the
/// provider <c>GetAttachmentAccessCheck</c> but replaces the provider three-prong with a single ownership gate
/// (<c>sr.OwnerUserId == the token owner</c>); the fileId-belongs-to-SR verification is IDENTICAL. Returns the
/// access-OK boolean only — the mobile BFF mints the signed read-url. Owner identity is taken from the token, never a
/// parameter.
/// </summary>
[DocumentationInfo("Get owner attachment access-check query", "Owner-scoped attachment access-check for a chat/attachment fileId on the owner's own SR.")]
public sealed class GetOwnerAttachmentAccessCheckQuery : AizenQuery<GetAttachmentAccessCheckResponse>
{
    public long ServiceRequestId { get; }
    public Guid FileId { get; }

    public GetOwnerAttachmentAccessCheckQuery(long serviceRequestId, Guid fileId)
    {
        ServiceRequestId = serviceRequestId;
        FileId = fileId;
    }
}
