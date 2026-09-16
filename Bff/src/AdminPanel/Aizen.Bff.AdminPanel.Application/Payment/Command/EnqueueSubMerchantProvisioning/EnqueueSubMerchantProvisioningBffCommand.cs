using Aizen.Bff.AdminPanel.Application.Payment.Dto;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.Payment.Command.EnqueueSubMerchantProvisioning;

/// <summary>Admin "approve billing / send to iyzico" — enqueues async sub-merchant provisioning and resets the retry
/// counter (re-trigger for capped/failed profiles). Idempotent: an already-keyed profile is returned as-is.</summary>
public sealed class EnqueueSubMerchantProvisioningBffCommand : AizenCommand<SubMerchantOnboardingMutateBffResponse>
{
    public long ProviderProfileId { get; init; }
}
