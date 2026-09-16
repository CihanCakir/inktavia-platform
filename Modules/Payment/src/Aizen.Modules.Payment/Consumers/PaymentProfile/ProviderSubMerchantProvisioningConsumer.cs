using Aizen.Core.Infrastructure.Exception;
using Aizen.Core.Messagebus.Abstraction.Consumers;
using Aizen.Core.Messagebus.Abstraction.Messages;
using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.Payment.Abstraction.Message;
using Aizen.Modules.Payment.Application.Commands.RegisterProviderSubMerchant;
using Aizen.Modules.Payment.Application.Services;
using Aizen.Modules.Payment.Domain.Interface.Repository;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Aizen.Modules.Payment.Consumers.PaymentProfile;

/// <summary>
/// Handles <see cref="ProviderSubMerchantProvisioningRequested"/>: runs the CANONICAL gateway-aware register path
/// (<see cref="RegisterProviderSubMerchantCommand"/> — manual mints a synthetic key, iyzico delegates to
/// RegisterSubMerchant). On success the profile advances to SubMerchantCreated (split-eligible). On a gateway/business
/// failure it records the attempt (time + error + count) on the profile, leaves the status DataSubmitted, logs a
/// Warning and ACKS — never dead-letters into a poison loop (the hourly sweep re-publishes until the attempt cap).
/// </summary>
public sealed class ProviderSubMerchantProvisioningConsumer
    : AizenBaseMessageConsumer<ProviderSubMerchantProvisioningRequested>
{
    private readonly IProviderPaymentProfileRepository _profiles;
    private readonly ISender                           _sender;
    private readonly string                            _encryptionKey;
    private readonly ILogger<ProviderSubMerchantProvisioningConsumer> _logger;

    public ProviderSubMerchantProvisioningConsumer(IServiceProvider sp) : base(sp)
    {
        _profiles      = sp.GetRequiredService<IProviderPaymentProfileRepository>();
        _sender        = sp.GetRequiredService<ISender>();
        _encryptionKey = sp.GetRequiredService<IConfiguration>()["Payment:IbanEncryptionKey"]
                         ?? "default-dev-key-change-in-prod!!";
        _logger        = sp.GetRequiredService<ILogger<ProviderSubMerchantProvisioningConsumer>>();
    }

    public override async Task<bool> ExecutePrepareMessage(
        ProviderSubMerchantProvisioningRequested message, CancellationToken ct)
    {
        var profile = await _profiles.GetByProviderProfileIdAsync(message.ProviderProfileId, ct);
        if (profile is null)
            return false;   // nothing to provision

        // Idempotent: an already-keyed profile is done.
        if (!string.IsNullOrWhiteSpace(profile.SubMerchantKey))
            return false;

        // Only DataSubmitted profiles are provisionable (skip Rejected/Suspended/Blocked/NotStarted).
        return profile.OnboardingStatus == ProviderSubMerchantOnboardingStatus.DataSubmitted;
    }

    public override async Task ExecuteCommitMessage(
        ProviderSubMerchantProvisioningRequested message, CancellationToken ct)
    {
        var profile = await _profiles.GetByProviderProfileIdAsync(message.ProviderProfileId, ct);
        if (profile is null || !string.IsNullOrWhiteSpace(profile.SubMerchantKey)) return;

        var kyc = SubMerchantKycPayload.Decrypt(profile.KycPayloadEncrypted, _encryptionKey);

        try
        {
            // Canonical, gateway-aware transition. LegalName/TaxNumber/Iban fall back to the profile inside the handler;
            // the remaining KYC comes from the (decrypted) blob captured at submit.
            await _sender.Send(new RegisterProviderSubMerchantCommand
            {
                ProviderProfileId = message.ProviderProfileId,
                Email             = kyc.Email,
                SubMerchantType   = kyc.SubMerchantType,
                TaxOffice         = kyc.TaxOffice,
                GsmNumber         = kyc.GsmNumber,
                ContactName       = kyc.ContactName,
                ContactSurname    = kyc.ContactSurname,
                IdentityNumber    = kyc.IdentityNumber,
            }, ct);

            _logger.LogInformation(
                "Sub-merchant provisioning succeeded for provider {Id} (source={Source}).",
                message.ProviderProfileId, message.Source);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // ANY gateway/business failure — record the attempt, keep DataSubmitted, ACK (no poison loop / dead-letter).
            // The hourly sweep re-publishes until the attempt cap. Cancellation is allowed to propagate.
            await RecordFailureAsync(message.ProviderProfileId, ex.Message, ct);
            _logger.LogWarning(ex,
                "Sub-merchant provisioning failed for provider {Id} (source={Source}); recorded attempt, will retry via sweep.",
                message.ProviderProfileId, message.Source);
        }
    }

    private async Task RecordFailureAsync(long providerProfileId, string? error, CancellationToken ct)
    {
        var profile = await _profiles.GetByProviderProfileIdAsync(providerProfileId, ct);
        if (profile is null || !string.IsNullOrWhiteSpace(profile.SubMerchantKey)) return;

        profile.RecordProvisioningFailure(error);
        _profiles.Update(profile);
        await _profiles.SaveChangesAsync(ct);   // consumers are NOT wrapped by the command decorator — save directly
    }

    public override Task ExecuteRollbackMessage(
        ProviderSubMerchantProvisioningRequested message, AizenMessageError ex, CancellationToken ct)
    {
        _logger.LogWarning("Rollback: ProviderSubMerchantProvisioningConsumer provider {Id}: {Error}",
            message.ProviderProfileId, ex.Message);
        return Task.CompletedTask;
    }
}
