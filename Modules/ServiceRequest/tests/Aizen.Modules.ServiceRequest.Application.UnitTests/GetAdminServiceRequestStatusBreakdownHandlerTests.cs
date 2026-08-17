using Aizen.Modules.ServiceRequest.Abstraction.Enum;
using Aizen.Modules.ServiceRequest.Abstraction.Request.Filter;
using Aizen.Modules.ServiceRequest.Abstraction.Response.Admin;
using Aizen.Modules.ServiceRequest.Abstraction.Response.Provider;
using Aizen.Modules.ServiceRequest.Application.Query.Admin;
using Aizen.Modules.ServiceRequest.Domain.Entities.Pricing;
using Aizen.Modules.ServiceRequest.Domain.Entities.ServiceRequest;
using Aizen.Modules.ServiceRequest.Domain.Interface.Repository;
using Aizen.Modules.ServiceRequest.Domain.ReadModel;
using FluentAssertions;

namespace Aizen.Modules.ServiceRequest.Application.UnitTests;

/// <summary>
/// C2 — the admin SR status-breakdown query. The repository returns the enum-typed (status → count) read-model;
/// the handler's job is to map each status to the lowercase key the BFF/FE consume (WaitingForOffer →
/// waitingforoffer) while preserving the counts. Uses a hand-written in-memory fake repository (the SR test
/// project carries no mocking library).
/// </summary>
public sealed class GetAdminServiceRequestStatusBreakdownHandlerTests
{
    // Minimal fake: only GetStatusBreakdownAsync is exercised; the rest of the interface throws.
    private sealed class FakeRepo : IServiceRequestRepository
    {
        private readonly IReadOnlyList<ServiceRequestStatusCount> _rows;
        public FakeRepo(params ServiceRequestStatusCount[] rows) => _rows = rows;

        public Task<IReadOnlyList<ServiceRequestStatusCount>> GetStatusBreakdownAsync(CancellationToken ct = default)
            => Task.FromResult(_rows);

        // ── unused ──────────────────────────────────────────────────────────────
        public Task<ServiceRequestEntity?> GetByIdAsync(long id, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<ServiceRequestEntity?> GetByIdWithDetailsAsync(long id, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<ServiceRequestEntity?> GetByCodeAsync(string requestCode, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<IReadOnlyList<ServiceRequestEntity>> GetByOwnerUserIdAsync(long ownerUserId, int skip, int take, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<int> CountByOwnerUserIdAsync(long ownerUserId, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<IReadOnlyList<ServiceRequestEntity>> GetByVesselIdAsync(long vesselId, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<IReadOnlyList<ServiceRequestEntity>> GetAdminListAsync(AdminServiceRequestFilterRequest filter, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<int> CountAdminAsync(AdminServiceRequestFilterRequest filter, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<GetAdminServiceRequestStatsResponse> GetAdminStatsAsync(CancellationToken ct = default) => throw new NotImplementedException();
        public Task<IReadOnlyList<ServiceRequestEntity>> GetOpenForProviderAsync(long providerProfileId, ProviderAvailableServiceRequestFilterRequest filter, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<int> CountOpenForProviderAsync(long providerProfileId, ProviderAvailableServiceRequestFilterRequest filter, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<List<ProviderDiscoveryItemDto>> GetDiscoveryAsync(long providerProfileId, ProviderServiceRequestDiscoveryFilter filter, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<List<DiscoveryMarkerDto>> GetDiscoveryMarkersAsync(long providerProfileId, ProviderServiceRequestDiscoveryFilter filter, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<ProviderDiscoverySummaryResponse> GetDiscoverySummaryAsync(long providerProfileId, ProviderServiceRequestDiscoveryFilter filter, CancellationToken ct = default) => throw new NotImplementedException();
        public Task AddAsync(ServiceRequestEntity entity, CancellationToken ct = default) => throw new NotImplementedException();
        public void Update(ServiceRequestEntity entity) => throw new NotImplementedException();
        public Task<IReadOnlyDictionary<long, TravelPricingDetailEntity>> GetTravelPricingByOfferItemIdsAsync(IReadOnlyCollection<long> offerItemIds, CancellationToken ct = default) => throw new NotImplementedException();
    }

    [Fact]
    public async Task Maps_enum_statuses_to_lowercase_keys_and_preserves_counts()
    {
        var repo = new FakeRepo(
            new ServiceRequestStatusCount { Status = ServiceRequestStatus.WaitingForOffer, Count = 5 },
            new ServiceRequestStatusCount { Status = ServiceRequestStatus.InProgress, Count = 3 },
            new ServiceRequestStatusCount { Status = ServiceRequestStatus.Completed, Count = 12 });
        var handler = new GetAdminServiceRequestStatusBreakdownQueryHandler(repo);

        var result = await handler.Handle(new GetAdminServiceRequestStatusBreakdownQuery(), default);

        result.Should().HaveCount(3);
        result.Should().ContainEquivalentOf(new ServiceRequestStatusCountDto { Status = "waitingforoffer", Count = 5 });
        result.Should().ContainEquivalentOf(new ServiceRequestStatusCountDto { Status = "inprogress", Count = 3 });
        result.Should().ContainEquivalentOf(new ServiceRequestStatusCountDto { Status = "completed", Count = 12 });
    }

    [Fact]
    public async Task Empty_breakdown_maps_to_empty_list()
    {
        var handler = new GetAdminServiceRequestStatusBreakdownQueryHandler(new FakeRepo());

        var result = await handler.Handle(new GetAdminServiceRequestStatusBreakdownQuery(), default);

        result.Should().BeEmpty();
    }
}
