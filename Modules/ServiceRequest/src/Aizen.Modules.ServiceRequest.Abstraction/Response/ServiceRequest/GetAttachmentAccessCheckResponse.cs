namespace Aizen.Modules.ServiceRequest.Abstraction.Response.ServiceRequest;

/// <summary>
/// Confirms a provider may access a specific attachment on a specific request.
/// Returns the FileId so the BFF can mint a signed URL.
/// </summary>
public sealed class GetAttachmentAccessCheckResponse
{
    public Guid FileId { get; init; }
    public bool Authorized { get; init; }

    public GetAttachmentAccessCheckResponse() { }
    public GetAttachmentAccessCheckResponse(Guid fileId, bool authorized)
    {
        FileId = fileId;
        Authorized = authorized;
    }
}
