using Aizen.Bff.MarineProvider.Application.Contracts.Auth;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.MarineProvider.Application.Auth.RegisterProvider;

/// <summary>
/// Email/password provider registration (Flow 1). Orchestrates Keycloak (full IdP) + Identity provisioning.
/// </summary>
public sealed class RegisterProviderCommand : AizenCommand<RegisterProviderResponse>
{
    public string Email { get; set; } = default!;
    public string Password { get; set; } = default!;
    public string CompanyName { get; set; } = default!;
    public string? TaxNo { get; set; }
    public string? ContactPhone { get; set; }
    public string OwnerFirstName { get; set; } = default!;
    public string OwnerLastName { get; set; } = default!;
    public bool KvkkAccepted { get; set; }
    public string? ReturnUrl { get; set; }
}
