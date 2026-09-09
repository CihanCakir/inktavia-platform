using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.Payment.Abstraction.Interface;
using Aizen.Modules.Payment.Application.Commands.RegisterProviderSubMerchant;
using Aizen.Modules.Payment.Application.Commands.RegisterSubMerchant;
using Aizen.Modules.Payment.Application.Services;
using Aizen.Modules.Payment.Domain.Entities.PaymentProfile;
using Aizen.Modules.Payment.Domain.Interface.Repository;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Aizen.Modules.Payment.Domain.UnitTests.PaymentProfile;

/// <summary>
/// #113 — the admin "register sub-merchant" step that advances DataSubmitted|Rejected → SubMerchantCreated.
/// Before this, a submitted provider profile was a dead end (nothing set SubMerchantKey), so IsSplitEligible never
/// held and offer acceptance always failed "provider not split-eligible". These pin: the manual/dev gateway mints a
/// synthetic key without iyzico, idempotency, the not-found + invalid-transition guards, and iyzico delegation.
/// </summary>
public sealed class RegisterProviderSubMerchantCommandHandlerTests
{
    private const long ProviderId = 4;

    // A profile as it exists right after the provider PUT /payment-profile: DataSubmitted, IBAN captured.
    private static ProviderPaymentProfileEntity DataSubmittedProfile()
    {
        var p = ProviderPaymentProfileEntity.Create(ProviderId, "manual", legalName: "Acme Marine Ltd", taxNumber: "1234567890");
        p.SubmitOnboardingData();          // NotStarted → DataSubmitted
        p.UpdateIban("enc-iban", "6672");  // IBAN captured at data-submit (required for IsSplitEligible)
        return p;
    }

    private static IConfiguration Config() =>
        new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Payment:IbanEncryptionKey"] = "unit-test-key-0000000000000000!!",
        }).Build();

    private static IPaymentGatewayResolver GatewayReturning(string key)
    {
        var provider = Substitute.For<IPaymentGatewayProvider>();
        provider.ProviderKey.Returns(key);
        var resolver = Substitute.For<IPaymentGatewayResolver>();
        resolver.Resolve().Returns(provider);
        return resolver;
    }

    private static RegisterProviderSubMerchantCommandHandler Build(
        IProviderPaymentProfileRepository repo, IPaymentGatewayResolver resolver, ISender sender) =>
        new(repo, resolver, sender, Config(), NullLogger<RegisterProviderSubMerchantCommandHandler>.Instance);

    [Fact]
    public async Task Manual_gateway_mints_synthetic_key_and_becomes_split_eligible()
    {
        var profile = DataSubmittedProfile();
        var repo = Substitute.For<IProviderPaymentProfileRepository>();
        repo.GetByProviderProfileIdAsync(ProviderId, Arg.Any<CancellationToken>()).Returns(profile);
        var sender = Substitute.For<ISender>();

        var handler = Build(repo, GatewayReturning("manual"), sender);
        var result = await handler.Handle(
            new RegisterProviderSubMerchantCommand { ProviderProfileId = ProviderId }, CancellationToken.None);

        profile.SubMerchantKey.Should().StartWith("manual-");                       // synthetic, clearly prefixed
        profile.OnboardingStatus.Should().Be(ProviderSubMerchantOnboardingStatus.SubMerchantCreated);
        result!.IsSplitEligible.Should().BeTrue();                                  // the whole point of #113
        result.OnboardingStatus.Should().Be(ProviderSubMerchantOnboardingStatus.SubMerchantCreated);
        await repo.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await sender.DidNotReceive().Send(Arg.Any<RegisterSubMerchantCommand>(), Arg.Any<CancellationToken>()); // no iyzico
    }

    [Fact]
    public async Task Idempotent_when_already_keyed_returns_existing_without_gateway_or_save()
    {
        var profile = DataSubmittedProfile();
        profile.MarkSubMerchantCreated("manual-existingkey", null);   // already registered
        var repo = Substitute.For<IProviderPaymentProfileRepository>();
        repo.GetByProviderProfileIdAsync(ProviderId, Arg.Any<CancellationToken>()).Returns(profile);
        var resolver = Substitute.For<IPaymentGatewayResolver>();
        var sender = Substitute.For<ISender>();

        var handler = Build(repo, resolver, sender);
        var result = await handler.Handle(
            new RegisterProviderSubMerchantCommand { ProviderProfileId = ProviderId }, CancellationToken.None);

        result!.OnboardingStatus.Should().Be(ProviderSubMerchantOnboardingStatus.SubMerchantCreated);
        result.IsSplitEligible.Should().BeTrue();
        resolver.DidNotReceive().Resolve();                                          // short-circuits before gateway
        await repo.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
        await sender.DidNotReceive().Send(Arg.Any<RegisterSubMerchantCommand>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Missing_profile_throws_not_found()
    {
        var repo = Substitute.For<IProviderPaymentProfileRepository>();
        repo.GetByProviderProfileIdAsync(ProviderId, Arg.Any<CancellationToken>())
            .Returns((ProviderPaymentProfileEntity?)null);

        var handler = Build(repo, GatewayReturning("manual"), Substitute.For<ISender>());

        var act = () => handler.Handle(
            new RegisterProviderSubMerchantCommand { ProviderProfileId = ProviderId }, CancellationToken.None);

        (await act.Should().ThrowAsync<AizenBusinessException>())
            .Which.ErrorCode.Should().Be((int)PaymentErrorCode.ProviderPaymentProfileNotFound);
    }

    [Fact]
    public async Task Invalid_transition_when_blocked_throws()
    {
        var profile = ProviderPaymentProfileEntity.Create(ProviderId, "manual");
        profile.Block();   // NotStarted → Blocked (no key) — MarkSubMerchantCreated is illegal from here
        var repo = Substitute.For<IProviderPaymentProfileRepository>();
        repo.GetByProviderProfileIdAsync(ProviderId, Arg.Any<CancellationToken>()).Returns(profile);

        var handler = Build(repo, GatewayReturning("manual"), Substitute.For<ISender>());

        var act = () => handler.Handle(
            new RegisterProviderSubMerchantCommand { ProviderProfileId = ProviderId }, CancellationToken.None);

        (await act.Should().ThrowAsync<AizenBusinessException>())
            .Which.ErrorCode.Should().Be((int)PaymentErrorCode.ProviderSubMerchantInvalidTransition);
    }

    [Fact]
    public async Task Iyzico_gateway_delegates_to_register_command()
    {
        var profile = DataSubmittedProfile();
        var repo = Substitute.For<IProviderPaymentProfileRepository>();
        repo.GetByProviderProfileIdAsync(ProviderId, Arg.Any<CancellationToken>()).Returns(profile);
        var sender = Substitute.For<ISender>();

        var handler = Build(repo, GatewayReturning("iyzico"), sender);
        await handler.Handle(new RegisterProviderSubMerchantCommand
        {
            ProviderProfileId = ProviderId,
            LegalName = "Acme", Email = "ops@acme.example", Iban = "TR000000000000000000000000",
        }, CancellationToken.None);

        await sender.Received(1).Send(
            Arg.Is<RegisterSubMerchantCommand>(c => c.ProviderProfileId == ProviderId && c.Email == "ops@acme.example"),
            Arg.Any<CancellationToken>());
        await sender.Received(1).Send(Arg.Any<RegisterSubMerchantCommand>(), Arg.Any<CancellationToken>());
    }
}
