using Aizen.Bff.MarineProvider.Application.Common.Services;
using Aizen.Core.Realtime.Abstraction.Interfaces;
using Aizen.Core.Realtime.Hubs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;

namespace Aizen.Bff.MarineProvider.Realtime;

/// <summary>
/// The provider portal's realtime channel — hosted on the BFF, not on a module.
///
/// Why here: the browser authenticates against the BFF with a Keycloak token; modules are reached with the BFF's
/// service token plus an identity assertion. Exposing a module hub straight to the browser would be the first time
/// the SPA talks to a module directly, and the group key (`sub` → provider profile id) would become a security
/// boundary we would have to get exactly right. Get it wrong once and a provider joins another provider's group and
/// watches their bids. Here, the identity is the one the BFF already resolved and trusts.
///
/// Group membership is decided by the SERVER, from the resolved profile — never from anything the client sends.
/// There is deliberately no "subscribe to group X" method on this hub.
///
/// Built on the framework's <see cref="DomainHubBase"/> (ADR: BFF-hosted edge on Aizen.Core.Realtime, modules
/// publish-only). The routing that used to live in per-event hand-rolled consumers now lives in the single
/// <see cref="ProviderEventSocketMapper"/>; this type is only the connection/identity boundary.
///
/// NOTE on group prefixes: the framework's socket manager routes a group broadcast to a hub by parsing the group
/// name's prefix (up to the first ':') as the domain key. This hub joins TWO prefixes — "provider:{id}" and
/// "city:{code}" — so it is registered under BOTH the "provider" and "city" domain keys in Program.cs. Without the
/// "city" registration every city-targeted event (ServiceRequestPublished/Updated/Cancelled/UrgencyChanged) would
/// silently fail to route. <see cref="DomainName"/> itself is informational and does not drive routing.
/// </summary>
[Authorize]
public sealed class ProviderRealtimeHub : DomainHubBase
{
    private readonly IProviderProfileResolver _resolver;
    private readonly IProviderIdentityHolder _identityHolder;
    private readonly ILogger<ProviderRealtimeHub> _logger;

    public ProviderRealtimeHub(
        IRealtimePublisher publisher,
        IProviderProfileResolver resolver,
        IProviderIdentityHolder identityHolder,
        ILogger<ProviderRealtimeHub> logger)
        : base(publisher)
    {
        _resolver = resolver;
        _identityHolder = identityHolder;
        _logger = logger;
    }

    public override string DomainName => "provider";

    /// <summary>Every event addressed to one provider.</summary>
    public static string ProviderGroup(long providerProfileId) => $"provider:{providerProfileId}";

    /// <summary>Requests published in a city. A provider joins the city they operate in — nothing else.</summary>
    /// <summary>
    /// The canonical form is the ReferenceData city code, uppercase (e.g. "35" for Izmir, "48" for Mugla).
    /// Normalise here so one typo upstream does not silently split the group.
    /// </summary>
    public static string CityGroup(string cityCode) => $"city:{cityCode.Trim().ToUpperInvariant()}";

    public override async Task OnConnectedAsync()
    {
        var resolution = await _resolver.ResolveAsync(Context.ConnectionAborted);
        var profileId = resolution.ProfileId ?? 0;

        if (profileId <= 0 || _identityHolder.UserId is not > 0)
        {
            // Fail closed: an unresolved connection joins nothing and is dropped. It must never fall back to a
            // default or "public" group.
            _logger.LogWarning("Realtime connection rejected: provider identity could not be resolved.");
            Context.Abort();
            return;
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, ProviderGroup(profileId));

        // The provider's operating city comes from their Identity profile, resolved server-side.
        var city = resolution.Profile?.City;
        if (!string.IsNullOrWhiteSpace(city))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, CityGroup(city));
            _logger.LogInformation(
                "Realtime connected: provider {ProfileId}, city {City}, connection {ConnectionId}.",
                profileId, city, Context.ConnectionId);
        }
        else
        {
            // This is not informational — it means the provider will NEVER receive ServiceRequestPublished
            // events. In production this is the difference between an empty screen and a working one.
            _logger.LogWarning(
                "Realtime connected WITHOUT city group: provider {ProfileId} has no City on their profile. " +
                "They will not receive new service request notifications. Connection {ConnectionId}.",
                profileId, Context.ConnectionId);
        }

        await base.OnConnectedAsync();
    }
}
