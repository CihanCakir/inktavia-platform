using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Core.Security;
using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.Payment.Abstraction.Model.Result;
using Aizen.Modules.Payment.Application.Commands.RegisterSubMerchant;
using Aizen.Modules.Payment.Application.Services;
using Aizen.Modules.Payment.Domain.Interface.Repository;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Aizen.Modules.Payment.Application.Commands.RegisterProviderSubMerchant;

/// <summary>
/// BE-I1 admin — the missing onboarding transition (#113): {DataSubmitted, Rejected} → SubMerchantCreated.
/// Without it a submitted provider profile is a dead end — nothing advances it, so <c>IsSplitEligible</c> never
/// holds and offer acceptance always fails "provider not split-eligible".
/// Mirrors the verify/reject admin actions: keyed only by <see cref="ProviderProfileId"/>; the optional KYC fields
/// are used ONLY on the iyzico gateway path (the manual/dev gateway ignores them).
/// </summary>
public sealed class RegisterProviderSubMerchantCommand : AizenCommand<ProviderSubMerchantOnboardingResult>
{
    public required long ProviderProfileId { get; init; }

    // Optional iyzico KYC payload — required only for the iyzico gateway (the manual gateway mints a synthetic key).
    public string? LegalName      { get; init; }
    public string? Email          { get; init; }
    public string? Iban           { get; init; }
    public string? SubMerchantType { get; init; }
    public string? TaxNumber      { get; init; }
    public string? TaxOffice      { get; init; }
    public string? GsmNumber      { get; init; }
    public string? ContactName    { get; init; }
    public string? ContactSurname { get; init; }
    public string? IdentityNumber { get; init; }
}

[DocumentationInfo("Admin register-sub-merchant command handler (BE-I1/#113)",
    "Advances a provider's sub-merchant onboarding to SubMerchantCreated. Gateway-aware: the manual/dev gateway " +
    "mints a synthetic 'manual-' key without calling iyzico; the iyzico gateway delegates to RegisterSubMerchantCommand " +
    "(the P9 iyzico path). Idempotent — an already-keyed profile is returned as-is.")]
public sealed class RegisterProviderSubMerchantCommandHandler
    : AizenCommandHandler<RegisterProviderSubMerchantCommand, ProviderSubMerchantOnboardingResult>
{
    private const string ManualGatewayKey = "manual";

    private readonly IProviderPaymentProfileRepository _profiles;
    private readonly IPaymentGatewayResolver           _gatewayResolver;
    private readonly ISender                           _sender;
    private readonly string                            _encryptionKey;
    private readonly ILogger<RegisterProviderSubMerchantCommandHandler> _logger;

    public RegisterProviderSubMerchantCommandHandler(
        IProviderPaymentProfileRepository profiles,
        IPaymentGatewayResolver           gatewayResolver,
        ISender                           sender,
        IConfiguration                    configuration,
        ILogger<RegisterProviderSubMerchantCommandHandler> logger)
    {
        _profiles        = profiles;
        _gatewayResolver = gatewayResolver;
        _sender          = sender;
        _encryptionKey   = configuration["Payment:IbanEncryptionKey"] ?? "default-dev-key-change-in-prod!!";
        _logger          = logger;
    }

    public override async Task<ProviderSubMerchantOnboardingResult?> Handle(
        RegisterProviderSubMerchantCommand request, CancellationToken ct)
    {
        var profile = await _profiles.GetByProviderProfileIdAsync(request.ProviderProfileId, ct)
            ?? throw new AizenBusinessException((int)PaymentErrorCode.ProviderPaymentProfileNotFound);

        // Idempotency (#113.3): an already-keyed profile is returned as-is — re-register never fails.
        if (profile.SubMerchantKey is { Length: > 0 })
        {
            _logger.LogInformation(
                "Provider {Id} already has a sub-merchant key — returning existing onboarding state (idempotent).",
                request.ProviderProfileId);
            return Result(profile);
        }

        var gatewayKey = _gatewayResolver.Resolve().ProviderKey;

        // Manual/dev gateway (#113.2): mint a synthetic, clearly-prefixed key — NO iyzico call.
        if (string.Equals(gatewayKey, ManualGatewayKey, StringComparison.OrdinalIgnoreCase))
        {
            var syntheticKey = $"manual-{Guid.NewGuid():N}";
            profile.MarkSubMerchantCreated(syntheticKey, null);   // guarded: {NotStarted,DataSubmitted,Rejected} → SubMerchantCreated
            _profiles.Update(profile);
            await _profiles.SaveChangesAsync(ct);

            _logger.LogInformation(
                "Provider {Id} registered on the manual gateway with synthetic sub-merchant key {Key}.",
                request.ProviderProfileId, syntheticKey);

            return Result(profile);
        }

        // Iyzico gateway: delegate to the existing register command (the P9 path — unchanged). It performs the
        // gateway call + MarkSubMerchantCreated + persist (its own command decorator commits). Source the KYC from
        // the request, falling back to what the profile captured at data-submit time.
        await _sender.Send(new RegisterSubMerchantCommand
        {
            ProviderProfileId = request.ProviderProfileId,
            LegalName         = request.LegalName ?? profile.LegalName ?? string.Empty,
            Email             = request.Email     ?? string.Empty,
            Iban              = request.Iban      ?? DecryptIban(profile.IbanEncrypted),
            SubMerchantType   = request.SubMerchantType ?? "PRIVATE_COMPANY",
            TaxNumber         = request.TaxNumber ?? profile.TaxNumber,
            TaxOffice         = request.TaxOffice,
            GsmNumber         = request.GsmNumber,
            ContactName       = request.ContactName,
            ContactSurname    = request.ContactSurname,
            IdentityNumber    = request.IdentityNumber,
        }, ct);

        // Re-read the profile mutated by the inner command to report the resulting onboarding state.
        var updated = await _profiles.GetByProviderProfileIdAsync(request.ProviderProfileId, ct)
            ?? throw new AizenBusinessException((int)PaymentErrorCode.ProviderPaymentProfileNotFound);

        return Result(updated);
    }

    private static ProviderSubMerchantOnboardingResult Result(Domain.Entities.PaymentProfile.ProviderPaymentProfileEntity p)
        => new(p.ProviderProfileId, p.OnboardingStatus, p.IsSplitEligible);

    private string DecryptIban(string? ibanEncrypted)
        => string.IsNullOrEmpty(ibanEncrypted) ? string.Empty : AizenSecurityHelper.DecryptByAES(ibanEncrypted, _encryptionKey);
}
