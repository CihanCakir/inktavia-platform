using System.Diagnostics;
using Aizen.Bff.MarineProvider.Application.Common.Options;
using Aizen.Bff.MarineProvider.Application.Common.RemoteClients;
using Aizen.Bff.MarineProvider.Application.Common.Services;
using Aizen.Bff.MarineProvider.Application.Common.Warnings;
using Aizen.Bff.MarineProvider.Application.Contracts.Auth;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.Identity.Abstraction.Dto.EmailVerification;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Aizen.Bff.MarineProvider.Application.Auth;

public sealed class RegisterProviderCommandHandler
    : AizenCommandHandler<RegisterProviderCommand, RegisterProviderResponse>
{
    private readonly IProviderKeycloakAdminClient _keycloak;
    private readonly IIdentityRemoteCall _identity;
    private readonly MarineProviderKeycloakOptions _options;
    private readonly ILogger<RegisterProviderCommandHandler> _logger;

    public RegisterProviderCommandHandler(
        IProviderKeycloakAdminClient keycloak,
        IIdentityRemoteCall identity,
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
            // FATAL: hesap kimlik sağlayıcıda oluşturulamadı → başarı DEĞİL. 502 fırlat ki arayüz 200'e bakıp
            // "Kayıt başarılı" diyemesin.
            throw Upstream(ex, "keycloak.createUser");
        }

        response.KeycloakUserId = keycloakUserId;

        // Provision / link the Identity Organizer profile (idempotent). Keycloak sub == Keycloak user id.
        // FATAL: provisioning başarısızsa kullanıcı Keycloak'ta VAR ama bizim DB'de YOK → hiç giriş yapamaz.
        // Bu bir UYARI değil HATA — sessizce "başarılı" dönmek "yarı-oluşmuş hesap"ı gizler. 502 fırlatırız.
        long? providerProfileId;
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

            providerProfileId = provision?.Body?.ProviderProfileId;
        }
        catch (Exception ex)
        {
            throw Upstream(ex, "identity.provision");
        }

        if (providerProfileId is not > 0)
            throw Upstream(null, "identity.provision: no provider profile id returned");

        response.ProviderProfileId = providerProfileId;

        // provider_profile_id attribute'u token'a taşınır. Yazımı başarısız olursa KURTARILABİLİR (yeniden
        // atanabilir; hesap zaten oluştu) → uyarı, fatal değil.
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

        try
        {
            await _keycloak.AssignRealmRoleAsync(keycloakUserId, _options.ProviderPendingRole, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Assigning provider_pending role failed for {UserId}.", keycloakUserId);
            response.Warnings.Add(ProviderBffWarning.CallFailed("Keycloak.AssignRole", ex.GetType().Name));
        }

        // Doğrulama e-postasını tetikle: uygulama akışı (Identity → yerleşik onay token'ı). Keycloak'ın
        // execute-actions-email'i (İngilizce "Update Your Account") KALDIRILDI — artık yalnızca bizim Türkçe
        // e-postamız gider. Gönderim başarısızsa KURTARILABİLİR (kullanıcı resend edebilir) → uyarı, fatal değil.
        try
        {
            await _identity.GenerateProviderEmailVerification(
                new GenerateProviderEmailVerificationRequest { Email = email });
            response.EmailVerificationRequired = true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Triggering email verification failed for {UserId}.", keycloakUserId);
            response.Warnings.Add(ProviderBffWarning.CallFailed("Identity.GenerateEmailVerification", ex.GetType().Name));
        }

        response.RegistrationStatus = alreadyExisted ? "AlreadyRegistered" : "PendingEmailVerification";
        response.Message = alreadyExisted
            ? "An account already exists for this email. If it is unverified, a new verification email has been sent."
            : "Provider account created. Please verify your email to continue.";

        return response;
    }

    /// <summary>
    /// Kurtarılamaz kayıt hatasını 502 olarak yüzeye çıkarır (asla 200 maskesi). Arayüz 200'e bakıp başarı
    /// diyemez; "hesap oluşturuldu" ile "hesap yarı-oluştu" ayrımı böyle korunur.
    /// </summary>
    private AizenUpstreamException Upstream(Exception? ex, string step)
    {
        var correlationId = Activity.Current?.TraceId.ToString() ?? Guid.NewGuid().ToString("N");
        _logger.LogError(ex,
            "[{Code}] Provider registration failed at {Step}. correlationId={CorrelationId}",
            AizenUpstreamException.StableCode, step, correlationId);
        return new AizenUpstreamException(correlationId);
    }
}
