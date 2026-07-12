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
    public string Message { get; set; } = string.Empty;
    public List<ProviderBffWarning> Warnings { get; set; } = new();
}
