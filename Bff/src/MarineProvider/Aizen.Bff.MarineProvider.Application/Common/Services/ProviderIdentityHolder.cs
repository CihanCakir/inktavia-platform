namespace Aizen.Bff.MarineProvider.Application.Common.Services;

/// <summary>
/// Per-request cache of the resolved provider identity. Populated by <see cref="IProviderProfileResolver"/>
/// and read by the outgoing auth handler to assert identity to downstream modules — avoids re-resolving and,
/// crucially, avoids recursion (the resolver's own Identity calls run before the holder is populated, so they
/// carry no assertion headers).
/// </summary>
public interface IProviderIdentityHolder
{
    bool Resolved { get; }
    long? UserId { get; }
    long? ProfileId { get; }
    void Set(long? userId, long? profileId);
}

internal sealed class ProviderIdentityHolder : IProviderIdentityHolder
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
