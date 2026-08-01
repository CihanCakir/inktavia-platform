namespace Aizen.Bff.AdminPanel.Application.Common.Services;

/// <summary>
/// Per-request cache of the resolved admin identity. A mirror of the MarineProvider BFF's
/// <c>IProviderIdentityHolder</c>, but UserId-only — an admin is not a provider, so there is no profile id. Populated
/// by <see cref="IAdminIdentityResolver"/> from the validated Keycloak principal and read by the outgoing auth handler
/// to assert the acting-admin id to downstream modules (audit only; authorization is via the service token's Admin role).
/// </summary>
public interface IAdminIdentityHolder
{
    bool Resolved { get; }
    long? UserId { get; }
    void Set(long? userId);
}

internal sealed class AdminIdentityHolder : IAdminIdentityHolder
{
    public bool Resolved { get; private set; }
    public long? UserId { get; private set; }

    public void Set(long? userId)
    {
        UserId = userId;
        Resolved = true;
    }
}
