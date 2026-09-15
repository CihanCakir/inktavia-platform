using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;
using Aizen.Modules.ServiceRequest.Abstraction.Dto;
using Aizen.Modules.ServiceRequest.Abstraction.Enum;
using Aizen.Modules.ServiceRequest.Domain.Interface.Repository;

namespace Aizen.Modules.ServiceRequest.Application.Query.Admin.GetCargoDrySupplyOrders;

/// <summary>
/// CargoDry supply v2 (F1) — admin cargo-orders fulfilment queue: CARGODRY_SUPPLY orders in AwaitingShipment / Shipped.
/// Status filter: "AwaitingShipment" | "Shipped" | "All" (default All = both).
/// </summary>
public sealed class GetCargoDrySupplyOrdersAdminQuery : AizenQuery<CargoDrySupplyOrderAdminListDto>
{
    public string? Status   { get; init; }
    public int     Page     { get; init; } = 1;
    public int     PageSize { get; init; } = 25;
}

public sealed class GetCargoDrySupplyOrdersAdminQueryHandler
    : AizenQueryHandler<GetCargoDrySupplyOrdersAdminQuery, CargoDrySupplyOrderAdminListDto>
{
    private readonly IServiceRequestRepository _repository;

    public GetCargoDrySupplyOrdersAdminQueryHandler(IServiceRequestRepository repository)
        => _repository = repository;

    public override async Task<CargoDrySupplyOrderAdminListDto?> Handle(
        GetCargoDrySupplyOrdersAdminQuery request, CancellationToken ct)
    {
        var statuses = request.Status?.Trim().ToLowerInvariant() switch
        {
            // Accept both the admin-web filter values (awaiting|shipped|all) and the full status names.
            "awaiting" or "awaitingshipment" => new[] { ServiceRequestStatus.AwaitingShipment },
            "shipped"                        => new[] { ServiceRequestStatus.Shipped },
            _                                => new[] { ServiceRequestStatus.AwaitingShipment, ServiceRequestStatus.Shipped }, // all
        };

        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = Math.Clamp(request.PageSize, 1, 100);
        var (items, total) = await _repository.GetCargoDrySupplyOrdersAsync(statuses, (page - 1) * pageSize, pageSize, ct);

        return new CargoDrySupplyOrderAdminListDto
        {
            Items = items.Select(x => new CargoDrySupplyOrderAdminDto
            {
                ServiceRequestId          = x.Id,
                RequestCode               = x.RequestCode,
                ProductCode               = x.CargoDryProductCode,
                VesselName                = x.VesselName,
                OwnerUserId               = x.OwnerUserId,
                OrderStatus               = x.Status.ToString(),
                // "Awaiting since" = when the order first entered AwaitingShipment; fall back to created time.
                AwaitingSince             = x.StatusHistory
                    .Where(h => h.ToStatus == ServiceRequestStatus.AwaitingShipment)
                    .OrderBy(h => h.OccurredAt)
                    .Select(h => (DateTime?)h.OccurredAt)
                    .FirstOrDefault() ?? x.CreateDate,
                ProviderAcceptDeadlineUtc = x.ProviderAcceptDeadlineUtc,
                ShippedAtUtc              = x.ShippedAtUtc,
                TrackingCode              = x.TrackingCode,
                RetailAmount              = x.CargoDryRetailAmount,
                CurrencyCode              = x.CargoDryRetailCurrency,
                CreatedAt                 = x.CreateDate ?? DateTime.UtcNow,
            }).ToList(),
            Total    = total,
            Page     = page,
            PageSize = pageSize,
        };
    }
}
