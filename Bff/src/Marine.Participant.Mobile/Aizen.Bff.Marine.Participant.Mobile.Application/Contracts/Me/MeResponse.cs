namespace Aizen.Bff.Marine.Participant.Mobile.Application.Contracts.Me;

/// <summary>Lightweight identity echo built from the verified Keycloak token claims (no downstream call).</summary>
public sealed class MeResponse
{
    public string? Subject { get; set; }
    public string? Email { get; set; }
    public string? Username { get; set; }
    public IReadOnlyList<string> Roles { get; set; } = new List<string>();
}
