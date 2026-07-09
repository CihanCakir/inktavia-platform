using Aizen.Bff.MarineProvider.Application.Common.Warnings;

namespace Aizen.Bff.MarineProvider.Application.Contracts.Auth;

public sealed class RegisterProviderResponse
{
    public string RegistrationStatus { get; set; } = default!;
    public string? KeycloakUserId { get; set; }
    public long? ProviderProfileId { get; set; }
    public bool EmailVerificationRequired { get; set; }
    public string Message { get; set; } = string.Empty;
    public List<ProviderBffWarning> Warnings { get; set; } = new();
}
