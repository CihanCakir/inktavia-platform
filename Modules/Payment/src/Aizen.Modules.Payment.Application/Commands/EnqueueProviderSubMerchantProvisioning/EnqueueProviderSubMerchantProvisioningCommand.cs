using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Core.Messagebus.Abstraction.Senders;
using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.Payment.Abstraction.Message;
using Aizen.Modules.Payment.Abstraction.Model.Result;
using Aizen.Modules.Payment.Domain.Interface.Repository;
using Microsoft.Extensions.Logging;

namespace Aizen.Modules.Payment.Application.Commands.EnqueueProviderSubMerchantProvisioning;

/// <summary>
/// Admin "approve billing / send to iyzico" — ENQUEUES async provisioning (reuses the canonical consumer → gateway-aware
/// RegisterProviderSubMerchant) instead of calling the gateway inline. Also the re-trigger for capped/failed profiles:
/// it resets the attempt counter/error so the sweep resumes. Idempotent — an already-keyed profile is returned as-is.
/// </summary>
public sealed class EnqueueProviderSubMerchantProvisioningCommand : AizenCommand<ProviderSubMerchantOnboardingResult>
{
    public required long ProviderProfileId { get; init; }
}

[DocumentationInfo("Admin enqueue sub-merchant provisioning (approve billing / re-trigger)",
    "Publishes ProviderSubMerchantProvisioningRequested and resets the retry counter so a capped/failed profile is picked up again. Idempotent when already keyed.")]
public sealed class EnqueueProviderSubMerchantProvisioningCommandHandler
    : AizenCommandHandler<EnqueueProviderSubMerchantProvisioningCommand, ProviderSubMerchantOnboardingResult>
{
    /// <summary>NOT transactional: commit the reset/DataSubmitted state BEFORE publishing (so the consumer sees committed
    /// data). No nested command; the single explicit SaveChanges is atomic.</summary>
    public override bool IsTransactional => false;

    private readonly IProviderPaymentProfileRepository _profiles;
    private readonly IAizenMessagePublisher            _publisher;
    private readonly ILogger<EnqueueProviderSubMerchantProvisioningCommandHandler> _logger;

    public EnqueueProviderSubMerchantProvisioningCommandHandler(
        IProviderPaymentProfileRepository profiles,
        IAizenMessagePublisher            publisher,
        ILogger<EnqueueProviderSubMerchantProvisioningCommandHandler> logger)
    {
        _profiles  = profiles;
        _publisher = publisher;
        _logger    = logger;
    }

    public override async Task<ProviderSubMerchantOnboardingResult?> Handle(
        EnqueueProviderSubMerchantProvisioningCommand request, CancellationToken ct)
    {
        var profile = await _profiles.GetByProviderProfileIdAsync(request.ProviderProfileId, ct)
            ?? throw new AizenBusinessException((int)PaymentErrorCode.ProviderPaymentProfileNotFound);

        // Idempotent: already provisioned → return as-is (no re-enqueue).
        if (!string.IsNullOrWhiteSpace(profile.SubMerchantKey))
            return new ProviderSubMerchantOnboardingResult(profile.ProviderProfileId, profile.OnboardingStatus, profile.IsSplitEligible);

        // Bring the profile back into the provisionable window: NotStarted/Rejected → DataSubmitted, then reset attempts.
        if (profile.OnboardingStatus is ProviderSubMerchantOnboardingStatus.NotStarted
                                     or ProviderSubMerchantOnboardingStatus.Rejected)
            profile.SubmitOnboardingData();
        if (profile.OnboardingStatus != ProviderSubMerchantOnboardingStatus.DataSubmitted)
            throw new AizenBusinessException((int)PaymentErrorCode.ProviderSubMerchantInvalidTransition);

        profile.ResetProvisioningAttempts();
        _profiles.Update(profile);
        await _profiles.SaveChangesAsync(ct);   // committed before publish (IsTransactional=false)

        _ = _publisher.PublishAsync(new ProviderSubMerchantProvisioningRequested
        {
            ProviderProfileId = profile.ProviderProfileId,
            AttemptNumber     = profile.AttemptCount,
            Source            = "admin",
        }, ct).ContinueWith(t =>
        {
            if (t.IsFaulted)
                _logger.LogWarning(t.Exception,
                    "Failed to publish admin-triggered provisioning for provider {Id}; the hourly sweep will retry.",
                    profile.ProviderProfileId);
        }, TaskContinuationOptions.OnlyOnFaulted);

        return new ProviderSubMerchantOnboardingResult(profile.ProviderProfileId, profile.OnboardingStatus, profile.IsSplitEligible);
    }
}
