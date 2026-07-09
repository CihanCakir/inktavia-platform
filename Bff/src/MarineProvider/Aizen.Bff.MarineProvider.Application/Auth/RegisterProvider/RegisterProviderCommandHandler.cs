using Aizen.Bff.MarineProvider.Application.Common.Options;
using Aizen.Bff.MarineProvider.Application.Common.RemoteClients;
using Aizen.Bff.MarineProvider.Application.Common.Services;
using Aizen.Bff.MarineProvider.Application.Common.Warnings;
using Aizen.Bff.MarineProvider.Application.Contracts.Auth;
using Aizen.Core.CQRS.Handler;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Aizen.Bff.MarineProvider.Application.Auth.RegisterProvider;

public sealed class RegisterProviderCommandHandler
    : AizenCommandHandler<RegisterProviderCommand, RegisterProviderResponse>
{
    private readonly IProviderKeycloakAdminClient _keycloak;
    private readonly IProviderIdentityRemoteCall _identity;
    private readonly MarineProviderKeycloakOptions _options;
    private readonly ILogger<RegisterProviderCommandHandler> _logger;

    public RegisterProviderCommandHandler(
        IProviderKeycloakAdminClient keycloak,
        IProviderIdentityRemoteCall identity,
        IOptions<MarineProviderKeycloakOptions> options,
        ILogger<RegisterProviderCommandHandler> logger)
    {
        _keycloak = keycloak;
        _identity = identity;
        _options = options.Value;
        _logger = logger;
    }

    public override async Task<RegisterProviderResponse?> Handle(
        RegisterProviderCommand request, CancellationToken cancellationToken)
    {
        var response = new RegisterProviderResponse();

        if (string.IsNullOrWhiteSpace(request.Email)
            || string.IsNullOrWhiteSpace(request.Password)
            || string.IsNullOrWhiteSpace(request.CompanyName)
            || string.IsNullOrWhiteSpace(request.OwnerFirstName)
            || string.IsNullOrWhiteSpace(request.OwnerLastName))
        {
            response.RegistrationStatus = "Invalid";
            response.Message = "Email, password, company name and owner name are required.";
            return response;
        }

        if (!request.KvkkAccepted)
        {
            response.RegistrationStatus = "Invalid";
            response.Message = "KVKK / terms acceptance is required.";
            return response;
        }

        var email = request.Email.Trim().ToLowerInvariant();

        string keycloakUserId;
        bool alreadyExisted;

        try
        {
            var existing = await _keycloak.FindUserByEmailAsync(email, cancellationToken);
            if (existing is not null)
            {
                keycloakUserId = existing.Id;
                alreadyExisted = true;
            }
            else
            {
                keycloakUserId = await _keycloak.CreateUserAsync(
                    new CreateKeycloakUserRequest(email, request.Password, request.OwnerFirstName, request.OwnerLastName, null),
                    cancellationToken);
                alreadyExisted = false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Keycloak user creation failed during provider registration.");
            response.RegistrationStatus = "Failed";
            response.Message = "Provider account could not be created at the identity provider. Please try again.";
            response.Warnings.Add(ProviderBffWarning.CallFailed("Keycloak", ex.GetType().Name));
            return response;
        }

        response.KeycloakUserId = keycloakUserId;

        // Provision / link the Identity Organizer profile (idempotent). Keycloak sub == Keycloak user id.
        try
        {
            var provision = await _identity.ProvisionFromKeycloak(new ProviderProvisionFromKeycloakRequest
            {
                KeycloakSubjectId = keycloakUserId,
                Email = email,
                FirstName = request.OwnerFirstName,
                LastName = request.OwnerLastName,
                CompanyName = request.CompanyName,
                ContactPhone = request.ContactPhone,
                TaxNo = request.TaxNo,
                EmailVerified = false
            });

            var providerProfileId = provision?.Body?.ProviderProfileId;
            if (providerProfileId is > 0)
            {
                response.ProviderProfileId = providerProfileId;

                try
                {
                    await _keycloak.SetUserAttributeAsync(
                        keycloakUserId,
                        _options.ProviderProfileIdAttributeName,
                        providerProfileId.Value.ToString(),
                        cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Writing provider_profile_id attribute failed for {UserId}.", keycloakUserId);
                    response.Warnings.Add(ProviderBffWarning.CallFailed("Keycloak.SetAttribute", ex.GetType().Name));
                }
            }
            else
            {
                response.Warnings.Add(ProviderBffWarning.CallFailed("Identity.Provision", "no provider profile id returned"));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Identity provisioning failed during provider registration for {UserId}.", keycloakUserId);
            response.Warnings.Add(ProviderBffWarning.CallFailed("Identity.Provision", ex.GetType().Name));
        }

        try
        {
            await _keycloak.AssignRealmRoleAsync(keycloakUserId, _options.ProviderPendingRole, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Assigning provider_pending role failed for {UserId}.", keycloakUserId);
            response.Warnings.Add(ProviderBffWarning.CallFailed("Keycloak.AssignRole", ex.GetType().Name));
        }

        try
        {
            await _keycloak.SendVerifyEmailAsync(keycloakUserId, cancellationToken);
            response.EmailVerificationRequired = true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Sending verify-email failed for {UserId}.", keycloakUserId);
            response.Warnings.Add(ProviderBffWarning.CallFailed("Keycloak.VerifyEmail", ex.GetType().Name));
        }

        response.RegistrationStatus = alreadyExisted ? "AlreadyRegistered" : "PendingEmailVerification";
        response.Message = alreadyExisted
            ? "An account already exists for this email. If it is unverified, a new verification email has been sent."
            : "Provider account created. Please verify your email to continue.";

        return response;
    }
}
