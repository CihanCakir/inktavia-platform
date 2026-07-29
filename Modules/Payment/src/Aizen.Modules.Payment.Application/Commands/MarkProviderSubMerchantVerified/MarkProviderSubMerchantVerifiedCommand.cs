using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.Payment.Abstraction.Model.Result;
using Aizen.Modules.Payment.Domain.Interface.Repository;

namespace Aizen.Modules.Payment.Application.Commands.MarkProviderSubMerchantVerified;

/// <summary>BE-I1 admin — completes onboarding review: SubMerchantCreated → Verified.</summary>
public sealed class MarkProviderSubMerchantVerifiedCommand : AizenCommand<ProviderSubMerchantOnboardingResult>
{
    public required long ProviderProfileId { get; init; }
}

public sealed class MarkProviderSubMerchantVerifiedCommandHandler
    : AizenCommandHandler<MarkProviderSubMerchantVerifiedCommand, ProviderSubMerchantOnboardingResult>
{
    private readonly IProviderPaymentProfileRepository _profiles;
    public MarkProviderSubMerchantVerifiedCommandHandler(IProviderPaymentProfileRepository profiles) => _profiles = profiles;

    public override async Task<ProviderSubMerchantOnboardingResult?> Handle(
        MarkProviderSubMerchantVerifiedCommand request, CancellationToken ct)
    {
        var profile = await _profiles.GetByProviderProfileIdAsync(request.ProviderProfileId, ct)
            ?? throw new AizenBusinessException((int)PaymentErrorCode.ProviderPaymentProfileNotFound);

        profile.MarkVerified();   // guarded: throws ProviderSubMerchantInvalidTransition if not SubMerchantCreated
        _profiles.Update(profile);
        await _profiles.SaveChangesAsync(ct);

        return new ProviderSubMerchantOnboardingResult(
            profile.ProviderProfileId, profile.OnboardingStatus, profile.IsSplitEligible);
    }
}
