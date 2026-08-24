using Aizen.Bff.MarineProvider.Application.Common.RemoteClients;
using Aizen.Bff.MarineProvider.Application.Common.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace Aizen.Bff.MarineProvider.Application.Common.Authorization;

public static class ProviderAuthorizationPolicies
{
    public const string ProviderAuthenticated = "ProviderAuthenticated";
    public const string ProviderPendingOrActive = "ProviderPendingOrActive";
    public const string ProviderActive = "ProviderActive";
    public const string ProviderRestrictedAware = "ProviderRestrictedAware";
    /// <summary>PROV-MVP-002 — ProviderActive AND actually enrolled in the CargoDry programme.</summary>
    public const string CargoDryParticipant = "CargoDryParticipant";
}

/// <summary>
/// The capability vocabulary published on GET /me/status. Closed set; the SPA mirrors it as a union type and
/// silently drops anything it does not recognise.
/// </summary>
public static class ProviderCapabilityNames
{
    public const string CargoDry = "CargoDry";
}

/// <summary>
/// Requirement enforcing that the provider is enrolled in the CargoDry programme — i.e. holds at least one
/// Active consignment agreement valid right now.
///
/// PROV-MVP-002: before this, `CargoDryController` carried only `ProviderActive`, which means "any approved,
/// active provider on the platform". All 17 CargoDry handlers asked `ProfileId is null or 0` — *who are you*,
/// never *are you allowed CargoDry*. A provider whose onboarding recorded
/// `CargoDryInterest.interested = false` received HTTP 200 from all nine read endpoints and could POST a real
/// stock request. Participation is a commercial relationship an admin sets up, not the onboarding answer.
/// </summary>
public sealed class CargoDryParticipantRequirement : IAuthorizationRequirement
{
}

internal sealed class CargoDryParticipantAuthorizationHandler
    : AuthorizationHandler<CargoDryParticipantRequirement>
{
    private readonly IProviderProfileResolver _resolver;
    private readonly ICargoDryRemoteCall _cargoDry;

    public CargoDryParticipantAuthorizationHandler(
        IProviderProfileResolver resolver, ICargoDryRemoteCall cargoDry)
    {
        _resolver = resolver;
        _cargoDry = cargoDry;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext authorizationContext, CargoDryParticipantRequirement requirement)
    {
        try
        {
            // MUST run first. The outgoing assertion headers (X-Aizen-User-Id / X-Aizen-Provider-Profile-Id) are
            // attached by MarineProviderBffAuthDelegatingHandler only once the per-request holder is populated,
            // and only the resolver populates it. Every CargoDry BFF handler calls this before its remote call;
            // an authorization handler runs BEFORE all of them, so without this the module receives no provider
            // identity, throws, and the policy would deny genuine participants.
            var resolution = await _resolver.ResolveAsync(CancellationToken.None);
            if (resolution.ProfileId is not { } profileId || profileId <= 0) return;

            var participation = await _cargoDry.GetProviderParticipation();
            if (participation?.Body?.IsParticipant == true)
                authorizationContext.Succeed(requirement);
        }
        catch
        {
            // Unreachable module → do not grant. Same discipline as ProviderProfileAuthorizationHandler: an
            // entitlement that fails OPEN is not an entitlement.
        }
    }
}

/// <summary>
/// Requirement enforcing that a linked provider profile exists and (optionally) is Approved + Active.
/// Status is read from Identity at RUNTIME so suspend/restrict takes effect immediately even while
/// a Keycloak token is still valid — token roles alone are never trusted for approval decisions.
/// </summary>
public sealed class ProviderProfileRequirement : IAuthorizationRequirement
{
    public bool RequireActive { get; }
    public ProviderProfileRequirement(bool requireActive) => RequireActive = requireActive;
}

internal sealed class ProviderProfileAuthorizationHandler : AuthorizationHandler<ProviderProfileRequirement>
{
    private readonly IProviderContext _context;
    private readonly IIdentityRemoteCall _identity;

    public ProviderProfileAuthorizationHandler(IProviderContext context, IIdentityRemoteCall identity)
    {
        _context = context;
        _identity = identity;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext authorizationContext, ProviderProfileRequirement requirement)
    {
        if (!_context.IsAuthenticated) return;

        var profileId = _context.ProviderProfileId;
        if (profileId is null or <= 0) return; // no linked profile → profile-scoped policies fail

        var keycloakSubject = _context.KeycloakSubject;
        if (string.IsNullOrWhiteSpace(keycloakSubject)) return;

        try
        {
            // Use by-subject (IdentityRead policy) — the by-id endpoint requires Admin role.
            var response = await _identity.GetOrganizerProfileByKeycloakSubject(keycloakSubject);
            var dto = response?.Body;
            if (dto is null) return;

            var isApproved = string.Equals(dto.ApprovalStatus, "Approved", StringComparison.OrdinalIgnoreCase);
            var isActive = string.Equals(dto.Status, "Active", StringComparison.OrdinalIgnoreCase);
            var isSuspended = string.Equals(dto.Status, "Suspended", StringComparison.OrdinalIgnoreCase);

            if (isSuspended) return; // suspended never passes any profile-scoped policy

            if (requirement.RequireActive)
            {
                if (isApproved && isActive)
                    authorizationContext.Succeed(requirement);
            }
            else
            {
                // pending-or-active: any non-suspended linked profile may reach status/onboarding endpoints
                authorizationContext.Succeed(requirement);
            }
        }
        catch
        {
            // Runtime status unavailable → do not grant. Endpoints degrade with a controlled response.
        }
    }
}

public static class ProviderAuthorizationExtensions
{
    public static IServiceCollection AddMarineProviderAuthorization(this IServiceCollection services)
    {
        services.AddScoped<IAuthorizationHandler, ProviderProfileAuthorizationHandler>();
        services.AddScoped<IAuthorizationHandler, CargoDryParticipantAuthorizationHandler>();

        services.AddAuthorization(options =>
        {
            // PROV-MVP-046 — there was no DefaultPolicy and no FallbackPolicy, so an endpoint added without an
            // [Authorize] attribute was ANONYMOUS. Nothing was accidentally public today, but that guarantee
            // rested entirely on per-controller discipline. Make the floor structural instead.
            options.FallbackPolicy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build();

            options.AddPolicy(ProviderAuthorizationPolicies.ProviderAuthenticated,
                p => p.RequireAuthenticatedUser());

            options.AddPolicy(ProviderAuthorizationPolicies.ProviderPendingOrActive, p =>
            {
                p.RequireAuthenticatedUser();
                p.AddRequirements(new ProviderProfileRequirement(requireActive: false));
            });

            options.AddPolicy(ProviderAuthorizationPolicies.ProviderActive, p =>
            {
                p.RequireAuthenticatedUser();
                p.AddRequirements(new ProviderProfileRequirement(requireActive: true));
            });

            // Restricted-aware: authenticated access; the endpoint/handler decides restricted behavior
            // against runtime Identity status.
            options.AddPolicy(ProviderAuthorizationPolicies.ProviderRestrictedAware,
                p => p.RequireAuthenticatedUser());

            // CargoDry: everything ProviderActive demands, PLUS actual enrolment in the programme.
            options.AddPolicy(ProviderAuthorizationPolicies.CargoDryParticipant, p =>
            {
                p.RequireAuthenticatedUser();
                p.AddRequirements(new ProviderProfileRequirement(requireActive: true));
                p.AddRequirements(new CargoDryParticipantRequirement());
            });
        });

        return services;
    }
}
