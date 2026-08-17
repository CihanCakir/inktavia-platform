using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.Payment.Abstraction.Model.Result;
using Aizen.Modules.Payment.Domain.Interface.Repository;

namespace Aizen.Modules.Payment.Application.Commands.RejectProviderSubMerchant;

/// <summary>BE-I1 admin — rejects a provider's onboarding (→ Rejected, not split-eligible).</summary>
public sealed class RejectProviderSubMerchantCommand : AizenCommand<ProviderSubMerchantOnboardingResult>
{
    public required long   ProviderProfileId { get; init; }
    public          string? Reason           { get; init; }
}

public sealed class RejectProviderSubMerchantCommandHandler
    : AizenCommandHandler<RejectProviderSubMerchantCommand, ProviderSubMerchantOnboardingResult>
{
    private readonly IProviderPaymentProfileRepository _profiles;
    public RejectProviderSubMerchantCommandHandler(IProviderPaymentProfileRepository profiles) => _profiles = profiles;

    public override async Task<ProviderSubMerchantOnboardingResult?> Handle(
        RejectProviderSubMerchantCommand request, CancellationToken ct)
    {
        var profile = await _profiles.GetByProviderProfileIdAsync(request.ProviderProfileId, ct)
            ?? throw new AizenBusinessException((int)PaymentErrorCode.ProviderPaymentProfileNotFound);

        profile.Reject(request.Reason);   // guarded: throws ProviderSubMerchantInvalidTransition if already Blocked
        _profiles.Update(profile);
        await _profiles.SaveChangesAsync(ct);

        return new ProviderSubMerchantOnboardingResult(
            profile.ProviderProfileId, profile.OnboardingStatus, profile.IsSplitEligible);
    }
}
