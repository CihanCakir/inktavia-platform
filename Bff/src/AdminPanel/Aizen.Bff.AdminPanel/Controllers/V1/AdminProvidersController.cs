using Aizen.Bff.AdminPanel.Application.AdminIdentity.Query;
using Aizen.Bff.AdminPanel.Application.AdminProviders.Dto;
using Aizen.Bff.AdminPanel.Application.AdminProviders.Query;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Abstraction;
using Aizen.Core.Infrastructure.Api;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Aizen.Bff.AdminPanel.Controllers.V1;

/// <summary>
/// Phase 28 — Admin Provider operational data endpoints.
/// All endpoints are read-only enrichment. No provider state is mutated.
/// No SR assignment logic, CargoDry stock, or Payment business rules are changed.
/// </summary>
[ApiController]
[Route("api/v1/admin-panel/providers")]
[Tags("Admin Panel - Providers")]
[Authorize(Policy = "AdminPanelAccess")]
public sealed class AdminProvidersController : AizenWebApiController
{
    private readonly IAizenCQRSProcessor _cqrs;

    public AdminProvidersController(
        IHttpContextAccessor httpContextAccessor,
        IAizenCQRSProcessor  cqrsProcessor)
        : base(httpContextAccessor)
    {
        _cqrs = cqrsProcessor;
    }

    /// <summary>
    /// GET /api/v1/admin-panel/providers/{providerProfileId}/service-requests
    /// Returns service requests assigned to the given provider.
    /// Phase 28B — SR panel enrichment.
    /// </summary>
    [HttpGet("{providerProfileId:long}/service-requests")]
    [ProducesResponseType(typeof(ProviderServiceRequestsBffResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<ProviderServiceRequestsBffResponse>> GetServiceRequests(
        long providerProfileId,
        [FromQuery] int pageIndex = 0,
        [FromQuery] int pageSize  = 10,
        CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync(
            new GetProviderServiceRequestsBffQuery(providerProfileId, pageIndex, pageSize), ct);
        return SetResponse(result);
    }

    /// <summary>
    /// GET /api/v1/admin-panel/providers/{providerProfileId}/finance-summary
    /// Returns active subscription + recent payout summary for the given provider.
    /// Phase 28D — Finance panel enrichment. Parallel Payment module calls.
    /// </summary>
    [HttpGet("{providerProfileId:long}/finance-summary")]
    [ProducesResponseType(typeof(ProviderFinanceSummaryBffResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<ProviderFinanceSummaryBffResponse>> GetFinanceSummary(
        long providerProfileId,
        [FromQuery] int payoutPageSize = 10,
        CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync(
            new GetProviderFinanceSummaryBffQuery(providerProfileId, payoutPageSize), ct);
        return SetResponse(result);
    }

    /// <summary>
    /// GET /api/v1/admin-panel/providers
    /// Provider directory alias — server-side filtered paged list of provider (organizer) profiles.
    /// Phase 29D — replaces client-side filtering on the Admin Web ProvidersPage.
    /// Delegates to SearchOrganizerProfilesQuery (same handler as /identity/organizers/profiles).
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedOrganizerProfileResult), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<PagedOrganizerProfileResult>> GetProviders(
        [FromQuery] int     pageIndex      = 0,
        [FromQuery] int     pageSize       = 20,
        [FromQuery] string? searchTerm     = null,
        [FromQuery] string? approvalStatus = null,
        [FromQuery] string? status         = null,
        [FromQuery] string? city           = null,
        [FromQuery] string? country        = null,
        CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync(
            new SearchOrganizerProfilesQuery(pageIndex, pageSize, searchTerm, approvalStatus, status, city, country), ct);
        return SetResponse(result);
    }
}
