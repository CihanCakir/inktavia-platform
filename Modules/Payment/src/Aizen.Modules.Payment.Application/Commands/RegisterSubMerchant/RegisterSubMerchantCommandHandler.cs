using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Payment.Abstraction;
using Aizen.Modules.Payment.Application.Gateway.Iyzico;
using Aizen.Modules.Payment.Application.Gateway.Iyzico.Models;
using Aizen.Modules.Payment.Domain.Entities.PaymentProfile;
using Aizen.Modules.Payment.Domain.Interface.Repository;
using Microsoft.Extensions.Logging;

namespace Aizen.Modules.Payment.Application.Commands.RegisterSubMerchant;

[DocumentationInfo("Register sub-merchant command handler",
    "Registers a provider as an Iyzico sub-merchant and stores the subMerchantKey on their payment profile.")]
public sealed class RegisterSubMerchantCommandHandler
    : AizenCommandHandler<RegisterSubMerchantCommand, RegisterSubMerchantResult>
{
    private readonly IProviderPaymentProfileRepository _profiles;
    private readonly IyzicoHttpClient                  _iyzicoClient;
    private readonly ILogger<RegisterSubMerchantCommandHandler> _logger;

    public RegisterSubMerchantCommandHandler(
        IProviderPaymentProfileRepository profiles,
        IyzicoHttpClient iyzicoClient,
        ILogger<RegisterSubMerchantCommandHandler> logger)
    {
        _profiles     = profiles;
        _iyzicoClient = iyzicoClient;
        _logger       = logger;
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

        // Build Iyzico sub-merchant request
        var iyzReq = new IyzicoSubMerchantRequest
        {
            Locale                = "tr",
            ConversationId        = $"SM-{request.ProviderProfileId}",
            SubMerchantExternalId = $"PROV-{request.ProviderProfileId}",
            SubMerchantType       = request.SubMerchantType,
            Address               = request.Address,
            ContactName           = request.ContactName ?? request.LegalName,
            ContactSurname        = request.ContactSurname ?? string.Empty,
            Email                 = request.Email,
            GsmNumber             = request.GsmNumber,
            Name                  = request.LegalName,
            Iban                  = request.Iban,
            TaxOffice             = request.TaxOffice,
            TaxNumber             = request.TaxNumber,
            LegalCompanyTitle     = request.LegalName,
            Currency              = "TRY",
        };

        var response = await _iyzicoClient.CreateSubMerchantAsync(iyzReq, ct);

        if (response is null || !response.IsSuccess || string.IsNullOrEmpty(response.SubMerchantKey))
        {
            var msg = response?.ErrorMessage ?? "Iyzico sub-merchant API returned null.";
            _logger.LogError(
                "Iyzico sub-merchant creation failed. ProviderProfileId={Id} Error={Error} Code={Code}",
                request.ProviderProfileId, msg, response?.ErrorCode);
            throw new InvalidOperationException(
                $"Failed to register sub-merchant with Iyzico: [{response?.ErrorCode}] {msg}");
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

            profile.RegisterSubMerchant(response.SubMerchantKey, null);
            await _profiles.AddAsync(profile, ct);
            await _profiles.SaveChangesAsync(ct);

            return new RegisterSubMerchantResult(
                request.ProviderProfileId,
                response.SubMerchantKey,
                "iyzico");
        }
        else
        {
            existing.RegisterSubMerchant(response.SubMerchantKey, null);
            _profiles.Update(existing);
            await _profiles.SaveChangesAsync(ct);

            return new RegisterSubMerchantResult(
                request.ProviderProfileId,
                response.SubMerchantKey,
                existing.GatewayProvider);
        }
    }
}
