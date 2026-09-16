using Aizen.Core.Messagebus.Abstraction.Messages;
using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.Payment.Abstraction.Message;
using Aizen.Modules.Payment.Application.Commands.RegisterProviderSubMerchant;
using Aizen.Modules.Payment.Consumers.PaymentProfile;
using Aizen.Modules.Payment.Domain.Entities.PaymentProfile;
using Aizen.Modules.Payment.Domain.Interface.Repository;
using FluentAssertions;
using MassTransit;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace Aizen.Modules.Payment.Domain.UnitTests.PaymentProfile;

/// <summary>
/// The async provisioning consumer (Payment host). Prepare gates to only-provisionable profiles; Commit dispatches the
/// CANONICAL RegisterProviderSubMerchant on success, and on ANY gateway/business failure records the attempt + ACKS
/// (never throws → no poison loop / dead-letter; the hourly sweep retries until the cap).
/// </summary>
public sealed class ProviderSubMerchantProvisioningConsumerTests
{
    private const long ProviderId = 55;

    private sealed class Harness
    {
        public IProviderPaymentProfileRepository Repo { get; } = Substitute.For<IProviderPaymentProfileRepository>();
        public ISender Sender { get; } = Substitute.For<ISender>();
        public ProviderSubMerchantProvisioningConsumer Consumer { get; }

        public Harness()
        {
            var services = new ServiceCollection();
            // Base AizenBaseMessageConsumer ctor resolves these three request clients — substitute them.
            services.AddSingleton(Substitute.For<IRequestClient<AizenPrepareMessage<ProviderSubMerchantProvisioningRequested>>>());
            services.AddSingleton(Substitute.For<IRequestClient<AizenCommitMessage<ProviderSubMerchantProvisioningRequested>>>());
            services.AddSingleton(Substitute.For<IRequestClient<AizenRollbackMessage<ProviderSubMerchantProvisioningRequested>>>());
            services.AddSingleton(Repo);
            services.AddSingleton(Sender);
            services.AddSingleton<IConfiguration>(new ConfigurationBuilder().AddInMemoryCollection(
                new Dictionary<string, string?> { ["Payment:IbanEncryptionKey"] = "unit-test-key-0000000000000000!!" }).Build());
            services.AddSingleton<ILogger<ProviderSubMerchantProvisioningConsumer>>(
                NullLogger<ProviderSubMerchantProvisioningConsumer>.Instance);

            Consumer = new ProviderSubMerchantProvisioningConsumer(services.BuildServiceProvider());
        }
    }

    private static ProviderPaymentProfileEntity DataSubmitted()
    {
        var p = ProviderPaymentProfileEntity.Create(ProviderId, "iyzico", "Acme", "1234567890");
        p.UpdateProfileAndResetVerification("enc", "7890", "Acme", "1234567890"); // → DataSubmitted
        return p;
    }

    private static ProviderSubMerchantProvisioningRequested Msg() =>
        new() { ProviderProfileId = ProviderId, Source = "submit" };

    // ── Prepare gate ─────────────────────────────────────────────────────────

    [Fact]
    public async Task Prepare_true_only_for_unkeyed_DataSubmitted_profile()
    {
        var h = new Harness();
        h.Repo.GetByProviderProfileIdAsync(ProviderId, Arg.Any<CancellationToken>()).Returns(DataSubmitted());

        (await h.Consumer.ExecutePrepareMessage(Msg(), CancellationToken.None)).Should().BeTrue();
    }

    [Fact]
    public async Task Prepare_false_when_missing_or_already_keyed()
    {
        var missing = new Harness();
        missing.Repo.GetByProviderProfileIdAsync(ProviderId, Arg.Any<CancellationToken>())
            .Returns((ProviderPaymentProfileEntity?)null);
        (await missing.Consumer.ExecutePrepareMessage(Msg(), CancellationToken.None)).Should().BeFalse();

        var keyedProfile = DataSubmitted();
        keyedProfile.MarkSubMerchantCreated("sm-key", "acc");
        var keyed = new Harness();
        keyed.Repo.GetByProviderProfileIdAsync(ProviderId, Arg.Any<CancellationToken>()).Returns(keyedProfile);
        (await keyed.Consumer.ExecutePrepareMessage(Msg(), CancellationToken.None)).Should().BeFalse();
    }

    // ── Commit ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Commit_success_dispatches_canonical_register_command()
    {
        var h = new Harness();
        h.Repo.GetByProviderProfileIdAsync(ProviderId, Arg.Any<CancellationToken>()).Returns(DataSubmitted());

        await h.Consumer.ExecuteCommitMessage(Msg(), CancellationToken.None);

        await h.Sender.Received(1).Send(
            Arg.Is<RegisterProviderSubMerchantCommand>(c => c.ProviderProfileId == ProviderId),
            Arg.Any<CancellationToken>());
        await h.Repo.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>()); // no failure recorded on success
    }

    [Fact]
    public async Task Commit_gateway_failure_records_attempt_and_acks_without_throwing()
    {
        var profile = DataSubmitted();
        var h = new Harness();
        h.Repo.GetByProviderProfileIdAsync(ProviderId, Arg.Any<CancellationToken>()).Returns(profile);
        h.Sender.Send(Arg.Any<RegisterProviderSubMerchantCommand>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("iyzico 500"));

        // Must NOT throw — the consumer swallows gateway/business errors and ACKS (no poison loop).
        var act = () => h.Consumer.ExecuteCommitMessage(Msg(), CancellationToken.None);
        await act.Should().NotThrowAsync();

        profile.AttemptCount.Should().Be(1);
        profile.LastAttemptError.Should().Contain("iyzico 500");
        profile.OnboardingStatus.Should().Be(ProviderSubMerchantOnboardingStatus.DataSubmitted); // stays provisionable
        await h.Repo.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
