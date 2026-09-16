using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.Payment.Domain.Entities.PaymentProfile;
using FluentAssertions;

namespace Aizen.Modules.Payment.Domain.UnitTests.PaymentProfile;

/// <summary>
/// Async provisioning attempt bookkeeping on the profile — the state the retry sweep and the consumer's failure path rely
/// on. RecordProvisioningFailure increments the counter + stamps the time + keeps DataSubmitted (so the sweep re-picks it);
/// success clears the last error; the admin re-trigger resets the counter; the (encrypted) KYC blob round-trips a set/clear.
/// </summary>
public sealed class SubMerchantProvisioningAttemptTests
{
    private static ProviderPaymentProfileEntity DataSubmitted()
    {
        var p = ProviderPaymentProfileEntity.Create(7, "iyzico", "Acme Marine Ltd", "1234567890");
        p.UpdateProfileAndResetVerification("enc-iban", "6672", "Acme Marine Ltd", "1234567890"); // → DataSubmitted
        return p;
    }

    [Fact]
    public void RecordProvisioningFailure_increments_stamps_and_keeps_DataSubmitted()
    {
        var p = DataSubmitted();

        p.RecordProvisioningFailure("gateway 500");

        p.AttemptCount.Should().Be(1);
        p.LastAttemptError.Should().Be("gateway 500");
        p.LastAttemptAtUtc.Should().NotBeNull();
        p.OnboardingStatus.Should().Be(ProviderSubMerchantOnboardingStatus.DataSubmitted); // stays provisionable for the sweep
        p.IsSplitEligible.Should().BeFalse();

        p.RecordProvisioningFailure("gateway 503");
        p.AttemptCount.Should().Be(2, "each failed attempt increments the cap counter");
    }

    [Fact]
    public void RecordProvisioningFailure_truncates_error_to_1000_chars()
    {
        var p = DataSubmitted();

        p.RecordProvisioningFailure(new string('x', 5000));

        p.LastAttemptError!.Length.Should().Be(1000);
    }

    [Fact]
    public void ResetProvisioningAttempts_clears_counter_and_error()
    {
        var p = DataSubmitted();
        p.RecordProvisioningFailure("boom");
        p.RecordProvisioningFailure("boom again");

        p.ResetProvisioningAttempts();

        p.AttemptCount.Should().Be(0);
        p.LastAttemptError.Should().BeNull();
    }

    [Fact]
    public void MarkSubMerchantCreated_clears_last_error_and_becomes_split_eligible()
    {
        var p = DataSubmitted();
        p.RecordProvisioningFailure("transient");

        p.MarkSubMerchantCreated("sm-key-123", "acc-1");

        p.OnboardingStatus.Should().Be(ProviderSubMerchantOnboardingStatus.SubMerchantCreated);
        p.LastAttemptError.Should().BeNull("a successful provisioning clears the last failure");
        p.IsSplitEligible.Should().BeTrue();
    }

    [Fact]
    public void SetKycPayload_round_trips_and_clears()
    {
        var p = DataSubmitted();

        p.SetKycPayload("enc-blob");
        p.KycPayloadEncrypted.Should().Be("enc-blob");

        p.SetKycPayload(null);
        p.KycPayloadEncrypted.Should().BeNull();
    }
}
