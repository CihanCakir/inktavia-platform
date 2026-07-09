using Aizen.Bff.MarineProvider.Application.Common.Options;
using Aizen.Bff.MarineProvider.Application.Common.RemoteClients;
using Aizen.Bff.MarineProvider.Application.Common.Services;
using Aizen.Bff.MarineProvider.Application.Common.Warnings;
using Aizen.Bff.MarineProvider.Application.Contracts.Auth;
using Aizen.Core.CQRS.Handler;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Aizen.Bff.MarineProvider.Application.Auth.EnsureProviderProfile;

public sealed class EnsureProviderProfileCommandHandler
    : AizenCommandHandler<EnsureProviderProfileCommand, EnsureProviderProfileResponse>
{
    private readonly IProviderContext _context;
    private readonly IProviderIdentityRemoteCall _identity;
    private readonly IProviderKeycloakAdminClient _keycloak;
    private readonly MarineProviderKeycloakOptions _options;
    private readonly ILogger<EnsureProviderProfileCommandHandler> _logger;

    public EnsureProviderProfileCommandHandler(
        IProviderContext context,
        IProviderIdentityRemoteCall identity,
        IProviderKeycloakAdminClient keycloak,
        IOptions<MarineProviderKeycloakOptions> options,
        ILogger<EnsureProviderProfileCommandHandler> logger)
    {
        _context = context;
        _identity = identity;
        _keycloak = keycloak;
        _options = options.Value;
        _logger = logger;
    }

    public override async Task<EnsureProviderProfileResponse?> Handle(
        EnsureProviderProfileCommand request, CancellationToken cancellationToken)
    {
        var response = new EnsureProviderProfileResponse
        {
            KeycloakSubject = _context.KeycloakSubject
        };

        if (!_context.IsAuthenticated || string.IsNullOrWhiteSpace(_context.KeycloakSubject))
        {
            response.Status = "Unauthenticated";
            response.Message = "A valid Keycloak token is required.";
            return response;
        }

        var sub = _context.KeycloakSubject!;
        long? resolvedProfileId = null;

        // 1) Existing linkage by Keycloak subject.
        try
        {
            var bySubject = await _identity.GetOrganizerProfileByKeycloakSubject(sub);
            resolvedProfileId = bySubject?.Body?.Id;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "by-subject lookup failed during ensure-profile for {Sub}.", sub);
            response.Warnings.Add(ProviderBffWarning.CallFailed("Identity.GetOrganizerProfileByKeycloakSubject", ex.GetType().Name));
        }

        // 2) Provision if not linked (idempotent; social first login).
        if (resolvedProfileId is null or <= 0)
        {
            try
            {
                var provision = await _identity.ProvisionFromKeycloak(new ProviderProvisionFromKeycloakRequest
                {
                    KeycloakSubjectId = sub,
                    Email = _context.Email ?? string.Empty,
                    FirstName = _context.FirstName,
                    LastName = _context.LastName,
                    EmailVerified = _context.EmailVerified
                });
                resolvedProfileId = provision?.Body?.ProviderProfileId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Identity provisioning failed during ensure-profile for {Sub}.", sub);
                response.Warnings.Add(ProviderBffWarning.CallFailed("Identity.Provision", ex.GetType().Name));
            }
        }

        if (resolvedProfileId is null or <= 0)
        {
            response.Status = "ProfileLinkRequired";
            response.Message = "Provider profile could not be resolved or provisioned. Please try again.";
            return response;
        }

        response.ProviderProfileId = resolvedProfileId;

        // 3) Ensure the provider_profile_id attribute exists on the Keycloak user (if not already in the token).
        if (!_context.HasProfileLink)
        {
            try
            {
                await _keycloak.SetUserAttributeAsync(
                    sub, _options.ProviderProfileIdAttributeName, resolvedProfileId.Value.ToString(), cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Writing provider_profile_id attribute failed during ensure-profile for {Sub}.", sub);
                response.Warnings.Add(ProviderBffWarning.CallFailed("Keycloak.SetAttribute", ex.GetType().Name));
            }
        }

        // 4) Ensure provider_pending role (idempotent).
        try
        {
            await _keycloak.AssignRealmRoleAsync(sub, _options.ProviderPendingRole, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Assigning provider_pending role failed during ensure-profile for {Sub}.", sub);
            response.Warnings.Add(ProviderBffWarning.CallFailed("Keycloak.AssignRole", ex.GetType().Name));
        }

        response.Status = "Linked";
        response.Message = "Provider profile is linked.";
        return response;
    }
}
