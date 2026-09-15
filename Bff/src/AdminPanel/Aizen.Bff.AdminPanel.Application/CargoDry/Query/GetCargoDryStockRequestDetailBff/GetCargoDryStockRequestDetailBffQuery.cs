using Aizen.Bff.AdminPanel.Application.CargoDry.Dto;
using Aizen.Bff.AdminPanel.Application.CargoDry.Query.GetCargoDryStockRequestsBff;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;
using Aizen.Core.Infrastructure.Exception;
using Microsoft.Extensions.Logging;

namespace Aizen.Bff.AdminPanel.Application.CargoDry.Query.GetCargoDryStockRequestDetailBff;

/// <summary>Admin stock-request detail — the request + full requester context (history, allocated batches, movements).</summary>
public sealed class GetCargoDryStockRequestDetailBffQuery : AizenQuery<CargoDryStockRequestAdminDetailBffResponse>
{
    public long RequestId { get; init; }
}

[DocumentationInfo("Get CargoDry stock-request detail (admin BFF)",
    "Returns the request plus the provider's requester context, request history, allocated batches, and CargoDry " +
    "inventory movements. Sub-reads degrade gracefully.")]
public sealed class GetCargoDryStockRequestDetailBffQueryHandler
    : AizenQueryHandler<GetCargoDryStockRequestDetailBffQuery, CargoDryStockRequestAdminDetailBffResponse>
{
    private const int HistoryCount = 20;

    private readonly ICargoDryRemoteCall _cargoDry;
    private readonly ICargoDryStockRequestContextEnricher _enricher;
    private readonly ILogger<GetCargoDryStockRequestDetailBffQueryHandler> _logger;

    public GetCargoDryStockRequestDetailBffQueryHandler(
        ICargoDryRemoteCall cargoDry, ICargoDryStockRequestContextEnricher enricher,
        ILogger<GetCargoDryStockRequestDetailBffQueryHandler> logger)
    {
        _cargoDry = cargoDry;
        _enricher = enricher;
        _logger   = logger;
    }

    public override async Task<CargoDryStockRequestAdminDetailBffResponse?> Handle(
        GetCargoDryStockRequestDetailBffQuery request, CancellationToken ct)
    {
        var req = await _cargoDry.GetStockRequestByIdAsync(request.RequestId, ct)
            ?? throw new AizenBusinessException($"Stock request {request.RequestId} not found.");

        var pid = req.ProviderProfileId;
        var contexts = await _enricher.BuildAsync(new[] { pid }, HistoryCount, ct);
        contexts.TryGetValue(pid, out var ctx);

        var detail = new CargoDryStockRequestAdminDetailBffResponse
        {
            Request        = req,
            Requester      = ctx,
            RequestHistory = ctx?.RecentRequests ?? new(),
        };

        try
        {
            detail.AllocatedBatches = await _cargoDry.GetBatchesAsync(1, 50, pid, "allocated", ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Allocated-batches read failed for provider {ProfileId} in stock-request detail.", pid);
        }

        try
        {
            detail.InventoryMovements = await _cargoDry.GetInventoryMovementsAsync(
                pid, null, null, null, null, null, 1, 50, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Inventory-movements read failed for provider {ProfileId} in stock-request detail.", pid);
        }

        return detail;
    }
}
