using Aizen.Core.CQRS.Handler;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Core.Security;
using Aizen.Modules.Payment.Abstraction.Dto;
using Aizen.Modules.Payment.Domain.Entities.PaymentProfile;
using Aizen.Modules.Payment.Domain.Interface.Repository;
using Microsoft.Extensions.Configuration;

namespace Aizen.Modules.Payment.Application.Commands.UpsertProviderPaymentProfile;

public sealed class UpsertProviderPaymentProfileCommandHandler
    : AizenCommandHandler<UpsertProviderPaymentProfileCommand, ProviderPaymentProfileDto>
{
    private readonly IProviderPaymentProfileRepository _repo;
    private readonly string _encryptionKey;

    public UpsertProviderPaymentProfileCommandHandler(
        IProviderPaymentProfileRepository repo,
        IConfiguration configuration)
    {
        _repo          = repo;
        _encryptionKey = configuration["Payment:IbanEncryptionKey"] ?? "default-dev-key-change-in-prod!!";
    }

    public override async Task<ProviderPaymentProfileDto?> Handle(
        UpsertProviderPaymentProfileCommand request, CancellationToken ct)
    {
        var iban = request.Iban?.Trim().ToUpperInvariant().Replace(" ", "") ?? "";

        // Validate TR IBAN format: TR + 24 digits = 26 chars
        if (iban.Length != 26 || !iban.StartsWith("TR") || !iban[2..].All(char.IsDigit))
            throw new AizenBusinessException("Invalid IBAN format. Turkish IBAN must be 26 characters: TR + 24 digits.");

        var ibanLast4     = iban[^4..];
        var ibanEncrypted = AizenSecurityHelper.EncryptByAES(iban, _encryptionKey);

        var entity = await _repo.GetByProviderProfileIdAsync(request.ProviderProfileId, ct);

        if (entity is null)
        {
            entity = ProviderPaymentProfileEntity.Create(
                request.ProviderProfileId, "manual", request.LegalName, request.TaxNumber);
            entity.UpdateIban(ibanEncrypted, ibanLast4);
            entity.UpdateProfileAndResetVerification(ibanEncrypted, ibanLast4, request.LegalName, request.TaxNumber);
            await _repo.AddAsync(entity, ct);
        }
        else
        {
            entity.UpdateProfileAndResetVerification(ibanEncrypted, ibanLast4, request.LegalName, request.TaxNumber);
            _repo.Update(entity);
        }

        await _repo.SaveChangesAsync(ct);

        return new ProviderPaymentProfileDto
        {
            GatewayProvider = entity.GatewayProvider,
            HasIban         = true,
            IbanMasked      = $"TR** **** **** {ibanLast4}",
            LegalName       = entity.LegalName,
            TaxNumberMasked = MaskTaxNumber(entity.TaxNumber),
            Status          = entity.Status,
            VerifiedAt      = entity.VerifiedAt,

            // ── BE-I1 onboarding + split-eligibility (post-upsert state; UpdateProfileAndResetVerification → DataSubmitted) ──
            IsSplitEligible      = entity.IsSplitEligible,
            OnboardingStatus     = entity.OnboardingStatus.ToString(),
            SubMerchantKeyMasked = MaskSubMerchantKey(entity.SubMerchantKey),
            IbanRequired         = false,                    // an IBAN was just saved
            SubMerchantType      = request.SubMerchantType,  // echo the submitted KYC type (reserved for registration)
            RejectionReason      = null,
        };
    }

    private static string? MaskSubMerchantKey(string? key)
        => string.IsNullOrEmpty(key) ? null : (key.Length <= 4 ? new string('*', key.Length) : $"****{key[^4..]}");

    private static string? MaskTaxNumber(string? taxNumber)
    {
        if (string.IsNullOrEmpty(taxNumber)) return null;
        if (taxNumber.Length <= 3) return new string('*', taxNumber.Length);
        return new string('*', taxNumber.Length - 3) + taxNumber[^3..];
    }
}
