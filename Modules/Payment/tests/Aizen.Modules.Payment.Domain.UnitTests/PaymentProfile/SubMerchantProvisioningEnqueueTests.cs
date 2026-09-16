using Aizen.Core.Messagebus.Abstraction.Messages;
using Aizen.Core.Messagebus.Abstraction.Senders;
using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.Payment.Abstraction.Message;
using Aizen.Modules.Payment.Application.Commands.EnqueueProviderSubMerchantProvisioning;
using Aizen.Modules.Payment.Application.Commands.SubmitProviderPaymentProfile;
using Aizen.Modules.Payment.Domain.Entities.PaymentProfile;
using Aizen.Modules.Payment.Domain.Interface.Repository;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Aizen.Modules.Payment.Domain.UnitTests.PaymentProfile;

/// <summary>
/// The two ENQUEUE seams of the async provisioning flow:
///   • provider submit (SubmitProviderPaymentProfileCommand) — captures KYC, persists DataSubmitted, publishes
///     ProviderSubMerchantProvisioningRequested{Source=submit} AFTER commit, and never calls the gateway inline.
///   • admin re-trigger (EnqueueProviderSubMerchantProvisioningCommand) — idempotent when already keyed; otherwise
///     resets the attempt counter and re-publishes {Source=admin}.
/// </summary>
public sealed class SubMerchantProvisioningEnqueueTests
{
    private const long ProviderId = 42;
    private const string ValidIban = "TR320010009999901234567890"; // TR + 24 digits

    // Hand-rolled publisher double (records + returns a completed task so the handler's fire-and-forget ContinueWith runs).
    private sealed class RecordingPublisher : IAizenMessagePublisher
    {
        public List<AizenBaseMessage> Published { get; } = new();
        public Task PublishAsync<T>(T message, CancellationToken ct = default) where T : AizenBaseMessage
        { Published.Add(message); return Task.CompletedTask; }
        public Task PublishRollbackAsync<T>(T message, CancellationToken ct = default) where T : AizenBaseMessage => Task.CompletedTask;
        public Task<TResponse> SendAsync<T, TResponse>(T message, CancellationToken ct = default)
            where T : AizenBaseMessage where TResponse : class => Task.FromResult<TResponse>(null!);
    }

    private static IConfiguration Config() =>
        new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Payment:IbanEncryptionKey"] = "unit-test-key-0000000000000000!!",
        }).Build();

    // ── Provider submit ──────────────────────────────────────────────────────

    [Fact]
    public async Task Submit_persists_DataSubmitted_and_publishes_provisioning_request()
    {
        var repo = Substitute.For<IProviderPaymentProfileRepository>();
        repo.GetByProviderProfileIdAsync(ProviderId, Arg.Any<CancellationToken>())
            .Returns((ProviderPaymentProfileEntity?)null); // first-time submit → Create path
        ProviderPaymentProfileEntity? added = null;
        await repo.AddAsync(Arg.Do<ProviderPaymentProfileEntity>(e => added = e), Arg.Any<CancellationToken>());
        var publisher = new RecordingPublisher();

        var handler = new SubmitProviderPaymentProfileCommandHandler(
            repo, publisher, Config(), NullLogger<SubmitProviderPaymentProfileCommandHandler>.Instance);

        var dto = await handler.Handle(new SubmitProviderPaymentProfileCommand
        {
            ProviderProfileId = ProviderId,
            Iban = ValidIban, LegalName = "Acme Marine Ltd", TaxNumber = "1234567890",
            Email = "ops@acme.example", SubMerchantType = "LIMITED_OR_JOINT_STOCK_COMPANY",
            TaxOffice = "Kadıköy", GsmNumber = "+905550000000",
            ContactName = "Ada", ContactSurname = "Denizci", IdentityNumber = "11111111111",
        }, CancellationToken.None);

        // committed before publish
        await repo.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        added!.OnboardingStatus.Should().Be(ProviderSubMerchantOnboardingStatus.DataSubmitted);
        added.KycPayloadEncrypted.Should().NotBeNullOrEmpty("KYC is captured encrypted for the async consumer");
        added.IbanLast4.Should().Be("7890");
        dto!.OnboardingStatus.Should().Be("DataSubmitted");
        dto.IsSplitEligible.Should().BeFalse("no sub-merchant key yet — provisioning is async");

        var msg = publisher.Published.OfType<ProviderSubMerchantProvisioningRequested>().Single();
        msg.ProviderProfileId.Should().Be(ProviderId);
        msg.Source.Should().Be("submit");
    }

    [Fact]
    public async Task Submit_rejects_non_TR_iban_before_any_persistence_or_publish()
    {
        var repo = Substitute.For<IProviderPaymentProfileRepository>();
        var publisher = new RecordingPublisher();
        var handler = new SubmitProviderPaymentProfileCommandHandler(
            repo, publisher, Config(), NullLogger<SubmitProviderPaymentProfileCommandHandler>.Instance);

        var act = () => handler.Handle(new SubmitProviderPaymentProfileCommand
        {
            ProviderProfileId = ProviderId, Iban = "GB29NWBK60161331926819",
        }, CancellationToken.None);

        await act.Should().ThrowAsync<Aizen.Core.Infrastructure.Exception.AizenBusinessException>();
        await repo.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
        publisher.Published.Should().BeEmpty();
    }

    // ── Admin re-trigger ─────────────────────────────────────────────────────

    [Fact]
    public async Task AdminEnqueue_already_keyed_is_idempotent_no_save_no_publish()
    {
        var profile = ProviderPaymentProfileEntity.Create(ProviderId, "iyzico", "Acme", "1234567890");
        profile.UpdateProfileAndResetVerification("enc", "7890", "Acme", "1234567890");
        profile.MarkSubMerchantCreated("sm-existing", "acc-1"); // already provisioned
        var repo = Substitute.For<IProviderPaymentProfileRepository>();
        repo.GetByProviderProfileIdAsync(ProviderId, Arg.Any<CancellationToken>()).Returns(profile);
        var publisher = new RecordingPublisher();

        var handler = new EnqueueProviderSubMerchantProvisioningCommandHandler(
            repo, publisher, NullLogger<EnqueueProviderSubMerchantProvisioningCommandHandler>.Instance);

        var result = await handler.Handle(
            new EnqueueProviderSubMerchantProvisioningCommand { ProviderProfileId = ProviderId }, CancellationToken.None);

        result!.IsSplitEligible.Should().BeTrue();
        await repo.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
        publisher.Published.Should().BeEmpty("an already-keyed profile is returned as-is");
    }

    [Fact]
    public async Task AdminEnqueue_capped_profile_resets_attempts_and_republishes()
    {
        var profile = ProviderPaymentProfileEntity.Create(ProviderId, "iyzico", "Acme", "1234567890");
        profile.UpdateProfileAndResetVerification("enc", "7890", "Acme", "1234567890"); // DataSubmitted
        for (var i = 0; i < 10; i++) profile.RecordProvisioningFailure("gateway down"); // hit the cap
        var repo = Substitute.For<IProviderPaymentProfileRepository>();
        repo.GetByProviderProfileIdAsync(ProviderId, Arg.Any<CancellationToken>()).Returns(profile);
        var publisher = new RecordingPublisher();

        var handler = new EnqueueProviderSubMerchantProvisioningCommandHandler(
            repo, publisher, NullLogger<EnqueueProviderSubMerchantProvisioningCommandHandler>.Instance);

        await handler.Handle(
            new EnqueueProviderSubMerchantProvisioningCommand { ProviderProfileId = ProviderId }, CancellationToken.None);

        profile.AttemptCount.Should().Be(0, "the re-trigger clears the cap counter so the sweep resumes");
        profile.LastAttemptError.Should().BeNull();
        await repo.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        var msg = publisher.Published.OfType<ProviderSubMerchantProvisioningRequested>().Single();
        msg.Source.Should().Be("admin");
    }
}
