using Aizen.Bff.MarineProvider.Application.Common.Warnings;

namespace Aizen.Bff.MarineProvider.Application.Contracts.Auth;

public sealed class EnsureProviderProfileResponse
{
    public string Status { get; set; } = default!;
    public string? KeycloakSubject { get; set; }
    public long? ProviderProfileId { get; set; }
    public string Message { get; set; } = string.Empty;
    public List<ProviderBffWarning> Warnings { get; set; } = new();
}
