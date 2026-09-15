using Aizen.Bff.AdminPanel.Application.CargoDry.Dto;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;
using Aizen.Modules.CargoDry.Abstraction.Dto;

namespace Aizen.Bff.AdminPanel.Application.CargoDry.Query.GetCargoDryStockRequestsBff;

/// <summary>Admin stock-request queue — paged (filters: status, providerProfileId) with per-row requester context.</summary>
public sealed class GetCargoDryStockRequestsBffQuery : AizenQuery<CargoDryStockRequestAdminListBffResponse>
{
    public int?  Status            { get; init; }
    public long? ProviderProfileId { get; init; }
    public int   Page              { get; init; } = 1;
    public int   PageSize          { get; init; } = 25;
}

[DocumentationInfo("Get CargoDry stock requests (admin BFF)",
    "Proxies the module admin stock-request list and enriches each row with requester context (provider name, active " +
    "agreements, allocated batches, recent requests). Per-provider enrichment failures degrade gracefully.")]
public sealed class GetCargoDryStockRequestsBffQueryHandler
    : AizenQueryHandler<GetCargoDryStockRequestsBffQuery, CargoDryStockRequestAdminListBffResponse>
{
    private const int RecentRequestCount = 5;

    private readonly ICargoDryRemoteCall _cargoDry;
    private readonly ICargoDryStockRequestContextEnricher _enricher;

    public GetCargoDryStockRequestsBffQueryHandler(
        ICargoDryRemoteCall cargoDry, ICargoDryStockRequestContextEnricher enricher)
    {
        _cargoDry = cargoDry;
        _enricher = enricher;
    }

    public override async Task<CargoDryStockRequestAdminListBffResponse?> Handle(
        GetCargoDryStockRequestsBffQuery request, CancellationToken ct)
    {
        var page = await _cargoDry.GetStockRequestsAsync(request.Status, request.ProviderProfileId, request.Page, request.PageSize, ct)
                   ?? new CargoDryStockRequestPagedResultDto { Page = request.Page, PageSize = request.PageSize };

        var contexts = await _enricher.BuildAsync(page.Items.Select(i => i.ProviderProfileId), RecentRequestCount, ct);

        return new CargoDryStockRequestAdminListBffResponse
        {
            Items = page.Items.Select(r => new CargoDryStockRequestAdminRowBffDto
            {
                Request   = r,
                Requester = contexts.TryGetValue(r.ProviderProfileId, out var c) ? c : null,
            }).ToList(),
            Total    = page.Total,
            Page     = page.Page,
            PageSize = page.PageSize,
        };
    }
}
