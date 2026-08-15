namespace Aizen.Bff.Marine.Web.Application.Common.Services;

/// <summary>
/// Per-request cache of the resolved participant identity. Read by the outgoing auth handler to assert identity
/// to downstream modules — avoids re-resolving and, crucially, avoids recursion (any resolver's own Identity
/// calls run before the holder is populated, so they carry no assertion headers).
/// Foundation only exposes the holder; full profile resolution arrives in a later phase.
/// </summary>
public interface IWebIdentityHolder
{
    bool Resolved { get; }
    long? UserId { get; }
    long? ProfileId { get; }
    void Set(long? userId, long? profileId);
}

internal sealed class WebIdentityHolder : IWebIdentityHolder
{
    public bool Resolved { get; private set; }
    public long? UserId { get; private set; }
    public long? ProfileId { get; private set; }

    public void Set(long? userId, long? profileId)
    {
        UserId = userId;
        ProfileId = profileId;
        Resolved = true;
    }
}
