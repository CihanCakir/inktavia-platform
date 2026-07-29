using Aizen.Core.CQRS.Handler;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Core.UnitOfWork.Abstraction;
using Aizen.Modules.Payment.Abstraction;
using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.Payment.Application.Gateway.Iyzico;
using Aizen.Modules.Payment.Application.Gateway.Iyzico.Models;
using Aizen.Modules.Payment.Domain.Entities.PaymentProfile;
using Aizen.Modules.Payment.Domain.Interface.Repository;
using Aizen.Modules.Payment.Repository.Persistence;
using Microsoft.Extensions.Logging;
using Aizen.Modules.Payment.Abstraction.Model.Result;

namespace Aizen.Modules.Payment.Application.Commands.RegisterSubMerchant;

[DocumentationInfo("Register sub-merchant command handler",
    "Registers a provider as an Iyzico sub-merchant and stores the subMerchantKey on their payment profile.")]
public sealed class RegisterSubMerchantCommandHandler
    : AizenCommandHandler<RegisterSubMerchantCommand, RegisterSubMerchantResult>
{
    private readonly IProviderPaymentProfileRepository          _profiles;
    private readonly IyzicoHttpClient                           _iyzicoClient;
    private readonly string                                     _encryptionKey;
    private readonly ILogger<RegisterSubMerchantCommandHandler> _logger;

    public RegisterSubMerchantCommandHandler(
        IAizenUnitOfWork<PaymentDbContext>               unitOfWork,
        IProviderPaymentProfileRepository                profiles,
        IyzicoHttpClient                                 iyzicoClient,
        Microsoft.Extensions.Configuration.IConfiguration configuration,
        ILogger<RegisterSubMerchantCommandHandler>       logger)
    {
        _profiles      = profiles;
        _iyzicoClient  = iyzicoClient;
        _encryptionKey = configuration["Payment:IbanEncryptionKey"] ?? "default-dev-key-change-in-prod!!";
        _logger        = logger;
    }

    // BE-P9-fix §5: store the IBAN on the profile so IsSplitEligible (which now requires an IBAN) holds after registration.
    private void StoreIban(Domain.Entities.PaymentProfile.ProviderPaymentProfileEntity profile, string iban)
    {
        var normalized = iban.Trim().ToUpperInvariant().Replace(" ", "");
        if (normalized.Length < 4) return;
        profile.UpdateIban(Aizen.Core.Security.AizenSecurityHelper.EncryptByAES(normalized, _encryptionKey), normalized[^4..]);
    }

    public override async Task<RegisterSubMerchantResult?> Handle(
        RegisterSubMerchantCommand request, CancellationToken ct)
    {
        // Upsert — if already registered, return existing key
        var existing = await _profiles.GetByProviderProfileIdAsync(request.ProviderProfileId, ct);
        if (existing?.SubMerchantKey is { Length: > 0 })
        {
            _logger.LogInformation(
                "Provider {Id} already has sub-merchant key — skipping re-registration.",
                request.ProviderProfileId);
            return new RegisterSubMerchantResult(
                request.ProviderProfileId,
                existing.SubMerchantKey,
                existing.GatewayProvider);
        }

        // BE-P9-fix §5: build a type-varied sub-merchant request (fail-loud on a missing required field; no hardcoded TCKN).
        var iyzReq = IyzicoSubMerchantRequestBuilder.Build(new SubMerchantOnboardingData(
            SubMerchantType:       request.SubMerchantType,
            SubMerchantExternalId: $"PROV-{request.ProviderProfileId}",
            Name:                  request.LegalName,
            Email:                 request.Email,
            Address:               request.Address,
            GsmNumber:             request.GsmNumber,
            ContactName:           request.ContactName ?? request.LegalName,
            ContactSurname:        request.ContactSurname,
            IdentityNumber:        request.IdentityNumber,
            TaxOffice:             request.TaxOffice,
            TaxNumber:             request.TaxNumber,
            LegalCompanyTitle:     request.LegalName,
            Iban:                  request.Iban,
            ConversationId:        $"SM-{request.ProviderProfileId}"));

        var response = await _iyzicoClient.CreateSubMerchantAsync(iyzReq, ct);

        if (response is null || !response.IsSuccess || string.IsNullOrEmpty(response.SubMerchantKey))
        {
            _logger.LogError(
                "Iyzico sub-merchant creation failed. ProviderProfileId={Id} Error={Error} Code={Code}",
                request.ProviderProfileId, response?.ErrorMessage, response?.ErrorCode);
            throw new AizenBusinessException((int)PaymentErrorCode.SubMerchantRegistrationFailed);
        }

        _logger.LogInformation(
            "Iyzico sub-merchant registered. ProviderProfileId={Id} SubMerchantKey={Key}",
            request.ProviderProfileId, response.SubMerchantKey);

        // Persist or update the provider payment profile
        if (existing is null)
        {
            var profile = ProviderPaymentProfileEntity.Create(
                providerProfileId: request.ProviderProfileId,
                gatewayProvider:   "iyzico",
                legalName:         request.LegalName,
                taxNumber:         request.TaxNumber);

            // BE-I1: advance the onboarding lifecycle → SubMerchantCreated (split-eligible). Idempotent.
            profile.MarkSubMerchantCreated(response.SubMerchantKey, null);
            StoreIban(profile, request.Iban);
            await _profiles.AddAsync(profile, ct);
            // SaveChanges is handled by AizenCommandHandlerDecorator — do NOT call here.

            return new RegisterSubMerchantResult(
                request.ProviderProfileId,
                response.SubMerchantKey,
                "iyzico");
        }
        else
        {
            // BE-I1: advance the onboarding lifecycle → SubMerchantCreated (split-eligible). Idempotent, no regression.
            existing.MarkSubMerchantCreated(response.SubMerchantKey, null);
            StoreIban(existing, request.Iban);
            _profiles.Update(existing);
            // SaveChanges is handled by AizenCommandHandlerDecorator — do NOT call here.

            return new RegisterSubMerchantResult(
                request.ProviderProfileId,
                response.SubMerchantKey,
                existing.GatewayProvider);
        }
    }
}
