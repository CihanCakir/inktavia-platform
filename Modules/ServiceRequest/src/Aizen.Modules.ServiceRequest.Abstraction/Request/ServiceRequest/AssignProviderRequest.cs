namespace Aizen.Modules.ServiceRequest.Abstraction.Request.ServiceRequest;

public sealed class AssignProviderRequest
{
    public long ProviderId { get; set; }
    public string ProviderName { get; set; } = string.Empty;
}
