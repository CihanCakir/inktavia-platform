using Aizen.Core.Cache.Abstraction;
using Aizen.Core.InfoAccessor.Abstraction;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Core.Messagebus.Abstraction.Senders;
using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.Payment.Abstraction.RemoteCall;
using Aizen.Modules.Payment.Abstraction.RemoteCall.Requests;
using Aizen.Modules.Payment.Abstraction.RemoteCall.Responses;
using Aizen.Modules.ServiceRequest.Abstraction.Enum;
using Aizen.Modules.ServiceRequest.Abstraction.Request.Offer;
using Aizen.Modules.ServiceRequest.Application.Command.Offer.SubmitOffer;
using Aizen.Modules.ServiceRequest.Application.Services;
using Aizen.Modules.ServiceRequest.Application.Services.Fx;
using Aizen.Modules.ServiceRequest.Domain.Entities.Offer;
using Aizen.Modules.ServiceRequest.Domain.Entities.ServiceRequest;
using Aizen.Modules.ServiceRequest.Domain.Interface.Repository;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace Aizen.Modules.ServiceRequest.Application.UnitTests;

/// <summary>
/// BE-I1 offer gate: a provider may SUBMIT an offer only when payment-split-eligible (completed payment profile →
/// sub-merchant created). The gate runs at submit — after the offer-state guards, before any economics — and calls
/// Payment for the eligibility signal. Not eligible ⇒ stable code SR_OFFER_PROVIDER_PAYMENT_PROFILE_REQUIRED; eligible ⇒
/// the submit proceeds past the gate (here it reaches the empty-offer guard, proving the gate let it through).
/// </summary>
public sealed class OfferSubmitSplitEligibilityGateTests
{
    private const long ProviderProfileId = 9001;
    private const long SrId = 500;
    private const long OfferId = 700;

    private static ServiceRequestEntity BiddableSr()
    {
        var sr = ServiceRequestEntity.Create(
            requestCode: "SR-GATE", ownerUserId: 100, vesselId: 5,
            serviceCategoryCode: "MECH", serviceTypeCode: null, title: "Engine service", description: null,
            priority: ServiceRequestPriority.Normal, requestedStartDate: null, requestedEndDate: null,
            locationCountryCode: "TR", locationCityCode: "IST", locationMarinaName: null,
            locationLatitude: null, locationLongitude: null, ownerNotes: null, expiresAt: null);
        sr.Publish();                                  // → Open (biddable)
        return sr;
    }

    // Draft offer owned by the caller, no priced lines (so an eligible caller lands on the empty-offer guard).
    private static ServiceRequestOfferEntity DraftOffer()
        => ServiceRequestOfferEntity.Create(SrId, ProviderProfileId, providerUserId: 7,
            totalAmount: 0m, currencyCode: "TRY", description: null, providerNotes: null,
            estimatedStartDate: null, estimatedEndDate: null, estimatedDurationMinutes: null, expiresAt: null);

    private static IAizenInfoAccessor Info()
    {
        var info = Substitute.For<IAizenInfoAccessor>();
        var kc = Substitute.For<IAizenKeycloakTokenInfoAccessor>();
        kc.KeycloakTokenInfo.Returns(new AizenKeycloakTokenInfo { ProviderProfileId = ProviderProfileId });
        info.KeycloakTokenInfoAccessor.Returns(kc);
        var users = Substitute.For<IAizenUserInfoAccessor>();
        users.UserInfo.Returns(new AizenUserInfo { AccessToken = "raw-token", UserId = 7 });
        info.UserInfoAccessor.Returns(users);
        return info;
    }

    private static SubmitOfferCommandHandler Build(IPaymentModuleRemoteCall payment)
    {
        var srRepo = Substitute.For<IServiceRequestRepository>();
        srRepo.GetByIdAsync(SrId, Arg.Any<CancellationToken>()).Returns(BiddableSr());
        var offerRepo = Substitute.For<IServiceRequestOfferRepository>();
        offerRepo.GetByIdAsync(OfferId, Arg.Any<CancellationToken>()).Returns(DraftOffer());

        var unitCode = new UnitCodeValidator(
            Substitute.For<Aizen.Modules.ServiceRequest.Abstraction.RemoteCall.IServiceRequestReferenceDataRemoteCall>(),
            Substitute.For<IAizenDistributedCache>(),
            NullLogger<UnitCodeValidator>.Instance);
        var fx = new OfferFxResolver(Substitute.For<IExchangeRateSource>());

        return new SubmitOfferCommandHandler(
            srRepo, offerRepo, Info(), new OfferCalculationService(), unitCode,
            Substitute.For<IAizenMessagePublisher>(), fx, payment);
    }

    private static SubmitOfferCommand Cmd() =>
        new(SrId, OfferId, new SubmitOfferRequest { IdempotencyKey = "k1" });

    [Fact]
    public async Task Rejects_submit_when_provider_is_not_split_eligible()
    {
        var payment = Substitute.For<IPaymentModuleRemoteCall>();
        payment.GetProviderSplitEligibilityAsync(
                Arg.Any<GetProviderSplitEligibilityRemoteCallRequest>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new GetProviderSplitEligibilityRemoteCallResponse
                { IsSplitEligible = false, OnboardingStatus = ProviderSubMerchantOnboardingStatus.DataSubmitted });

        var handler = Build(payment);

        var act = () => handler.Handle(Cmd(), CancellationToken.None);

        (await act.Should().ThrowAsync<AizenBusinessException>())
            .Which.Message.Should().Be("SR_OFFER_PROVIDER_PAYMENT_PROFILE_REQUIRED");

        await payment.Received(1).GetProviderSplitEligibilityAsync(
            Arg.Is<GetProviderSplitEligibilityRemoteCallRequest>(r => r.ProviderProfileId == ProviderProfileId),
            Arg.Is<string>(t => t.Contains("raw-token")), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Allows_submit_past_the_gate_when_split_eligible()
    {
        var payment = Substitute.For<IPaymentModuleRemoteCall>();
        payment.GetProviderSplitEligibilityAsync(
                Arg.Any<GetProviderSplitEligibilityRemoteCallRequest>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new GetProviderSplitEligibilityRemoteCallResponse
                { IsSplitEligible = true, OnboardingStatus = ProviderSubMerchantOnboardingStatus.SubMerchantCreated });

        var handler = Build(payment);

        // Eligible ⇒ the gate lets it through; the very next guard (no priced lines) fires instead of the gate code.
        var act = () => handler.Handle(Cmd(), CancellationToken.None);

        (await act.Should().ThrowAsync<AizenBusinessException>())
            .Which.Message.Should().Be("SR_OFFER_EMPTY");
    }
}
