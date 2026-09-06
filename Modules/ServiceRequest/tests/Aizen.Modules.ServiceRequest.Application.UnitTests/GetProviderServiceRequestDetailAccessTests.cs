using Aizen.Core.InfoAccessor.Abstraction;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.ServiceRequest.Abstraction.Enum;
using Aizen.Modules.ServiceRequest.Application.Query.Provider.GetProviderServiceRequestDetail;
using Aizen.Modules.ServiceRequest.Domain.Entities.Assignment;
using Aizen.Modules.ServiceRequest.Domain.Entities.Offer;
using Aizen.Modules.ServiceRequest.Domain.Entities.ServiceRequest;
using Aizen.Modules.ServiceRequest.Domain.Interface.Repository;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace Aizen.Modules.ServiceRequest.Application.UnitTests;

/// <summary>
/// Access guard for the provider SR detail READ.
///
/// Regression for the terminal-state 400: a provider deep-linking / refreshing on a request that has since reached a
/// terminal state (Cancelled/Completed/Expired/Closed) must get a readable 200 payload, NOT a business 400. The
/// relaxation is scoped to requests that were PUBLISHED (already public to city providers) — a never-published draft
/// that was cancelled stays private, and the biddable / offer / assignment prongs are unchanged.
/// </summary>
public sealed class GetProviderServiceRequestDetailAccessTests
{
    private const long ProviderProfileId = 9001;
    private const long QueriedSrId = 42;

    private static ServiceRequestEntity BuildSr(ServiceRequestStatus status, bool published)
    {
        var sr = ServiceRequestEntity.Create(
            requestCode: "SR-TEST",
            ownerUserId: 100,
            vesselId: 5,
            serviceCategoryCode: "MECH",
            serviceTypeCode: null,
            title: "Engine service",
            description: null,
            priority: ServiceRequestPriority.Normal,
            requestedStartDate: null,
            requestedEndDate: null,
            locationCountryCode: "TR",
            locationCityCode: "IST",
            locationMarinaName: null,
            locationLatitude: null,
            locationLongitude: null,
            ownerNotes: null,
            expiresAt: null);

        if (published) sr.Publish();          // → Open + PublishedAt set
        sr.ChangeStatus(status);               // land on the target lifecycle state
        return sr;
    }

    private static GetProviderServiceRequestDetailQueryHandler BuildHandler(
        ServiceRequestEntity sr,
        bool providerHasOffer = false,
        bool providerAssigned = false,
        long providerProfileId = ProviderProfileId)
    {
        var srRepo = Substitute.For<IServiceRequestRepository>();
        srRepo.GetByIdWithDetailsAsync(QueriedSrId, Arg.Any<CancellationToken>()).Returns(sr);

        var offerRepo = Substitute.For<IServiceRequestOfferRepository>();
        IReadOnlyList<ServiceRequestOfferEntity> offers = providerHasOffer
            ? new List<ServiceRequestOfferEntity>
            {
                ServiceRequestOfferEntity.Create(QueriedSrId, providerProfileId, providerUserId: 7,
                    totalAmount: 1000m, currencyCode: "TRY", description: null, providerNotes: null,
                    estimatedStartDate: null, estimatedEndDate: null, estimatedDurationMinutes: null, expiresAt: null)
            }
            : new List<ServiceRequestOfferEntity>();
        offerRepo.GetByServiceRequestIdAsync(Arg.Any<long>(), Arg.Any<CancellationToken>()).Returns(offers);

        var assignmentRepo = Substitute.For<IServiceRequestAssignmentRepository>();
        ServiceRequestAssignmentEntity? assignment = providerAssigned
            ? ServiceRequestAssignmentEntity.Create(QueriedSrId, serviceRequestOfferId: 1, providerProfileId,
                providerUserId: 7, assignedTeamMemberId: null, scheduledStartDate: null, scheduledEndDate: null)
            : null;
        assignmentRepo.GetByServiceRequestIdAsync(Arg.Any<long>(), Arg.Any<CancellationToken>()).Returns(assignment);

        var info = Substitute.For<IAizenInfoAccessor>();
        var kc = Substitute.For<IAizenKeycloakTokenInfoAccessor>();
        kc.KeycloakTokenInfo.Returns(new AizenKeycloakTokenInfo { ProviderProfileId = providerProfileId });
        info.KeycloakTokenInfoAccessor.Returns(kc);

        return new GetProviderServiceRequestDetailQueryHandler(
            srRepo, offerRepo, assignmentRepo, info,
            NullLogger<GetProviderServiceRequestDetailQueryHandler>.Instance);
    }

    private static GetProviderServiceRequestDetailQuery Query() => new(QueriedSrId);

    [Theory]
    [InlineData(ServiceRequestStatus.Cancelled)]
    [InlineData(ServiceRequestStatus.Completed)]
    [InlineData(ServiceRequestStatus.Expired)]
    [InlineData(ServiceRequestStatus.Closed)]
    public async Task Published_Then_Terminal_Is_Readable_By_Any_Provider(ServiceRequestStatus terminal)
    {
        // Provider has NO offer and NO assignment — the deep-link / refresh case that used to 400.
        var sr = BuildSr(terminal, published: true);
        var handler = BuildHandler(sr, providerHasOffer: false, providerAssigned: false);

        var result = await handler.Handle(Query(), CancellationToken.None);

        result.Should().NotBeNull();
        result!.Detail.Request.Status.Should().Be(terminal);   // readable terminal payload, not a rejection
    }

    [Fact]
    public async Task NeverPublished_Cancelled_Stays_Private_For_A_Stranger()
    {
        // A draft the owner cancelled before publishing: never public → a provider with no relationship cannot read it.
        var sr = BuildSr(ServiceRequestStatus.Cancelled, published: false);
        var handler = BuildHandler(sr, providerHasOffer: false, providerAssigned: false);

        var act = () => handler.Handle(Query(), CancellationToken.None);

        await act.Should().ThrowAsync<AizenBusinessException>().WithMessage("Service request not found.");
    }

    [Fact]
    public async Task NonTerminal_NonBiddable_Draft_Still_Rejected_Without_Relationship()
    {
        // Guard intact: a private in-flight (non-biddable, non-terminal) request is not leaked to unrelated providers.
        var sr = BuildSr(ServiceRequestStatus.Draft, published: false);
        var handler = BuildHandler(sr, providerHasOffer: false, providerAssigned: false);

        var act = () => handler.Handle(Query(), CancellationToken.None);

        await act.Should().ThrowAsync<AizenBusinessException>().WithMessage("Service request not found.");
    }

    [Fact]
    public async Task Cancelled_With_Own_Offer_Remains_Readable()
    {
        // The relationship prong still works on a terminal request (regression guard).
        var sr = BuildSr(ServiceRequestStatus.Cancelled, published: true);
        var handler = BuildHandler(sr, providerHasOffer: true, providerAssigned: false);

        var result = await handler.Handle(Query(), CancellationToken.None);

        result.Should().NotBeNull();
        result!.Detail.Request.Status.Should().Be(ServiceRequestStatus.Cancelled);
    }

    [Fact]
    public async Task Open_Biddable_Is_Readable_Baseline()
    {
        var sr = BuildSr(ServiceRequestStatus.Open, published: true);
        var handler = BuildHandler(sr, providerHasOffer: false, providerAssigned: false);

        var result = await handler.Handle(Query(), CancellationToken.None);

        result.Should().NotBeNull();
        result!.Detail.Request.Status.Should().Be(ServiceRequestStatus.Open);
    }
}
