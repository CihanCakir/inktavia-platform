using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.Payment.Domain.Entities.PaymentProfile;
using FluentAssertions;

namespace Aizen.Modules.Payment.Domain.UnitTests.PaymentProfile;

/// <summary>
/// BE-I1 — the provider sub-merchant onboarding lifecycle + the <c>IsSplitEligible</c> gate signal on the existing
/// <see cref="ProviderPaymentProfileEntity"/>. Legacy string <c>Status</c> mirrored throughout.
/// </summary>
public sealed class ProviderSubMerchantOnboardingTests
{
    private static ProviderPaymentProfileEntity NewProfile()
        => ProviderPaymentProfileEntity.Create(providerProfileId: 42, gatewayProvider: "iyzico");

    private static ProviderPaymentProfileEntity Created()
    {
        var p = NewProfile();
        p.MarkSubMerchantCreated("SM-KEY", "ACC-1");
        p.UpdateIban("enc-iban", "6672");   // BE-P9-fix §5: an IBAN is required for split-eligibility
        return p;
    }

    // ── Full happy lifecycle ────────────────────────────────────────────────────

    [Fact]
    public void Lifecycle_NotStarted_To_Verified()
    {
        var p = NewProfile();
        p.OnboardingStatus.Should().Be(ProviderSubMerchantOnboardingStatus.NotStarted);
        p.Status.Should().Be("Active");
        p.IsSplitEligible.Should().BeFalse();

        p.SubmitOnboardingData();
        p.OnboardingStatus.Should().Be(ProviderSubMerchantOnboardingStatus.DataSubmitted);
        p.Status.Should().Be("OnHold");
        p.IsSplitEligible.Should().BeFalse();

        p.MarkSubMerchantCreated("SM-KEY", "ACC-1");
        p.OnboardingStatus.Should().Be(ProviderSubMerchantOnboardingStatus.SubMerchantCreated);
        p.Status.Should().Be("Active");
        p.SubMerchantKey.Should().Be("SM-KEY");
        p.VerifiedAt.Should().BeNull();               // not verified yet
        p.IsSplitEligible.Should().BeFalse();         // BE-P9-fix §5: no IBAN yet → not split-eligible
        p.UpdateIban("enc", "6672");
        p.IsSplitEligible.Should().BeTrue();          // key + IBAN + SubMerchantCreated → eligible

        p.MarkVerified();
        p.OnboardingStatus.Should().Be(ProviderSubMerchantOnboardingStatus.Verified);
        p.VerifiedAt.Should().NotBeNull();
        p.IsSplitEligible.Should().BeTrue();
    }

    // ── Illegal transitions throw ProviderSubMerchantInvalidTransition ───────────

    [Fact]
    public void MarkVerified_From_NotStarted_Throws()
    {
        var act = () => NewProfile().MarkVerified();
        act.Should().Throw<AizenBusinessException>();
    }

    [Fact]
    public void SubmitOnboardingData_From_Verified_Throws()
    {
        var p = Created(); p.MarkVerified();
        var act = () => p.SubmitOnboardingData();
        act.Should().Throw<AizenBusinessException>();
    }

    [Fact]
    public void Suspend_From_NotStarted_Throws()
    {
        var act = () => NewProfile().Suspend();
        act.Should().Throw<AizenBusinessException>();
    }

    // ── Suspend / Reactivate ────────────────────────────────────────────────────

    [Fact]
    public void Suspend_Then_Reactivate_Restores_Created()
    {
        var p = Created();
        p.Suspend();
        p.OnboardingStatus.Should().Be(ProviderSubMerchantOnboardingStatus.Suspended);
        p.Status.Should().Be("OnHold");
        p.IsSplitEligible.Should().BeFalse();

        p.Reactivate();
        p.OnboardingStatus.Should().Be(ProviderSubMerchantOnboardingStatus.SubMerchantCreated);
        p.IsSplitEligible.Should().BeTrue();
    }

    [Fact]
    public void Suspend_Then_Reactivate_Restores_Verified_When_VerifiedAtSet()
    {
        var p = Created(); p.MarkVerified();
        p.Suspend();
        p.Reactivate();
        p.OnboardingStatus.Should().Be(ProviderSubMerchantOnboardingStatus.Verified);
        p.IsSplitEligible.Should().BeTrue();
    }

    // ── Block / Reject terminal ─────────────────────────────────────────────────

    [Fact]
    public void Block_Makes_Ineligible_And_Second_Block_Throws()
    {
        var p = Created();
        p.Block();
        p.OnboardingStatus.Should().Be(ProviderSubMerchantOnboardingStatus.Blocked);
        p.Status.Should().Be("Blocked");
        p.IsSplitEligible.Should().BeFalse();

        var act = () => p.Block();
        act.Should().Throw<AizenBusinessException>();
    }

    [Fact]
    public void Reject_Makes_Ineligible_And_Reject_From_Blocked_Throws()
    {
        var p = Created();
        p.Reject("KYC failed");
        p.OnboardingStatus.Should().Be(ProviderSubMerchantOnboardingStatus.Rejected);
        p.IsSplitEligible.Should().BeFalse();

        var blocked = Created(); blocked.Block();
        var act = () => blocked.Reject("x");
        act.Should().Throw<AizenBusinessException>();
    }

    [Fact]
    public void Rejected_Can_ReSubmit_Onboarding()
    {
        var p = Created(); p.Reject("fix docs");
        p.SubmitOnboardingData();      // Rejected → DataSubmitted allowed
        p.OnboardingStatus.Should().Be(ProviderSubMerchantOnboardingStatus.DataSubmitted);
    }

    // ── UpdateProfileAndResetVerification → back to DataSubmitted ────────────────

    [Fact]
    public void UpdateProfile_Resets_To_DataSubmitted_And_Clears_Verification()
    {
        var p = Created(); p.MarkVerified();
        p.UpdateProfileAndResetVerification("enc", "1234", "Acme", "TX1");
        p.OnboardingStatus.Should().Be(ProviderSubMerchantOnboardingStatus.DataSubmitted);
        p.Status.Should().Be("OnHold");
        p.VerifiedAt.Should().BeNull();
        p.IsSplitEligible.Should().BeFalse();
    }

    // ── IsSplitEligible matrix ──────────────────────────────────────────────────

    [Fact]
    public void IsSplitEligible_Matrix()
    {
        NewProfile().IsSplitEligible.Should().BeFalse();                       // NotStarted, no key

        Created().IsSplitEligible.Should().BeTrue();                           // key + SubMerchantCreated

        var verified = Created(); verified.MarkVerified();
        verified.IsSplitEligible.Should().BeTrue();                            // key + Verified

        var suspended = Created(); suspended.Suspend();
        suspended.IsSplitEligible.Should().BeFalse();

        var blocked = Created(); blocked.Block();
        blocked.IsSplitEligible.Should().BeFalse();

        var rejected = Created(); rejected.Reject(null);
        rejected.IsSplitEligible.Should().BeFalse();
    }

    // ── Registration wiring: MarkSubMerchantCreated from NotStarted + idempotency ─

    [Fact]
    public void MarkSubMerchantCreated_FromNotStarted_IsEligible_And_Idempotent()
    {
        var p = NewProfile();
        p.MarkSubMerchantCreated("SM-KEY", null);
        p.UpdateIban("enc", "6672");
        p.OnboardingStatus.Should().Be(ProviderSubMerchantOnboardingStatus.SubMerchantCreated);
        p.IsSplitEligible.Should().BeTrue();

        // idempotent re-call with the same key — no throw, no regression
        p.MarkSubMerchantCreated("SM-KEY", null);
        p.OnboardingStatus.Should().Be(ProviderSubMerchantOnboardingStatus.SubMerchantCreated);

        // idempotent even after Verified (same key) — must not drop back to Created
        p.MarkVerified();
        p.MarkSubMerchantCreated("SM-KEY", null);
        p.OnboardingStatus.Should().Be(ProviderSubMerchantOnboardingStatus.Verified);
    }
}
