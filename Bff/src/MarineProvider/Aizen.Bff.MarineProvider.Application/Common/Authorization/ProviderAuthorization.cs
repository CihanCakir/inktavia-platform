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
    private readonly IProviderIdentityRemoteCall _identity;

    public ProviderProfileAuthorizationHandler(IProviderContext context, IProviderIdentityRemoteCall identity)
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

        try
        {
            var response = await _identity.GetOrganizerProfileById(profileId.Value);
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

        services.AddAuthorization(options =>
        {
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
        });

        return services;
    }
}
