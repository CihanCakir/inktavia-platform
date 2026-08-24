using Aizen.Bff.MarineProvider.Application.Common.Warnings;

namespace Aizen.Bff.MarineProvider.Application.Contracts.Me;

public sealed class GetProviderStatusResponse
{
    public long? ProviderProfileId { get; set; }
    public string? KeycloakSubject { get; set; }
    public string? Email { get; set; }
    public string? CompanyName { get; set; }
    public string? OwnerName { get; set; }
    public string? ApprovalStatus { get; set; }
    public string? ProfileStatus { get; set; }
    public bool EmailVerified { get; set; }
    public bool? PhoneVerified { get; set; }
    public bool CanEnterWorkspace { get; set; }
    public string? OnboardingStatus { get; set; }
    public string RequiredNextStep { get; set; } = default!;

    /// <summary>
    /// What this provider is entitled to use — a closed vocabulary, currently { "CargoDry" }.
    ///
    /// PROV-MVP-002/003: the provider state model had no capability dimension at all, which is why CargoDry was
    /// served to a provider who declined it, why three Keycloak `provider_*` roles authorize nothing, and why two
    /// policies in this assembly are used by no endpoint. This list tells the SPA what to render; it is never the
    /// enforcement point — every gated endpoint checks independently, so a tampered client list grants nothing.
    ///
    /// Empty on a failed lookup (with a Warning attached): gating fails CLOSED without breaking the workspace.
    /// </summary>
    public List<string> Capabilities { get; set; } = new();
    public string Message { get; set; } = string.Empty;
    public List<ProviderBffWarning> Warnings { get; set; } = new();
}
