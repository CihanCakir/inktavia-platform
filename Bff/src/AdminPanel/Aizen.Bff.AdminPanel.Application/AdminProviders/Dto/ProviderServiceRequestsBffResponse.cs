using Aizen.Bff.AdminPanel.Application.ServiceRequests.Dto;
using Aizen.Bff.AdminPanel.Application.Common.Warnings;

namespace Aizen.Bff.AdminPanel.Application.AdminProviders.Dto;

[DocumentationInfo("Provider service requests BFF response",
    "Paged service request list for the provider operational detail panel. " +
    "Filtered by ProviderProfileId — returns only SR records assigned to this provider.")]
public sealed class ProviderServiceRequestsBffResponse
{
    public ServiceRequestPageBffDto? ServiceRequests { get; set; }
    public List<AdminBffWarning>     Warnings        { get; set; } = new();
}
