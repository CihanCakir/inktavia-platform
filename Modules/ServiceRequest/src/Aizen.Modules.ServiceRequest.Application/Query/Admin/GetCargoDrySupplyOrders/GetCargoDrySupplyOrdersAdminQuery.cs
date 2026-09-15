using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;
using Aizen.Modules.ServiceRequest.Abstraction.Dto;
using Aizen.Modules.ServiceRequest.Abstraction.Enum;
using Aizen.Modules.ServiceRequest.Domain.Interface.Repository;

namespace Aizen.Modules.ServiceRequest.Application.Query.Admin.GetCargoDrySupplyOrders;

/// <summary>
/// CargoDry supply v2 — admin cargo-orders + history: CARGODRY_SUPPLY orders across their lifecycle.
/// Status filter: "all" (every status incl. completed/cancelled) | "awaiting" | "shipped" | "completed" | "cancelled".
/// Optional OwnerUserId narrows to a single owner — the cargo "what did this owner buy" purchase history.
/// </summary>
public sealed class GetCargoDrySupplyOrdersAdminQuery : AizenQuery<CargoDrySupplyOrderAdminListDto>
{
    public string? Status      { get; init; }
    public long?   OwnerUserId { get; init; }
    public int     Page        { get; init; } = 1;
    public int     PageSize    { get; init; } = 25;
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
        // null ⇒ no status filter (full history). Accepts admin-web filter values + full status names.
        IReadOnlyList<ServiceRequestStatus>? statuses = request.Status?.Trim().ToLowerInvariant() switch
        {
            "awaiting" or "awaitingshipment" => new[] { ServiceRequestStatus.AwaitingShipment },
            "shipped"                        => new[] { ServiceRequestStatus.Shipped },
            // Completed cargo/provider orders settle to Completed then Closed — include both.
            "completed"                      => new[] { ServiceRequestStatus.Completed, ServiceRequestStatus.Closed },
            "cancelled" or "canceled"        => new[] { ServiceRequestStatus.Cancelled },
            "all" or null or ""              => null,
            _                                => null,
        };

        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = Math.Clamp(request.PageSize, 1, 100);
        var (items, total) = await _repository.GetCargoDrySupplyOrdersAsync(
            statuses, request.OwnerUserId, (page - 1) * pageSize, pageSize, ct);

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
                // Order path: a program provider was assigned ⇒ provider-fulfilled; otherwise the accept window
                // lapsed and it settled as a cargo/direct-online sale. (The authoritative direct-sale record lives
                // in the CargoDry module; the assignment presence is the SR-local, deploy-independent discriminator.)
                OrderPath                 = x.Assignment != null ? "ProviderFulfilled" : "CargoDirectSale",
                ProviderProfileId         = x.Assignment != null ? x.Assignment.ProviderProfileId : null,
                ProviderName              = x.AssignedProviderName,
                // Completed-at from the terminal transition (no dedicated column); prefer Completed, else Closed.
                CompletedAtUtc            = x.StatusHistory
                    .Where(h => h.ToStatus == ServiceRequestStatus.Completed || h.ToStatus == ServiceRequestStatus.Closed)
                    .OrderByDescending(h => h.OccurredAt)
                    .Select(h => (DateTime?)h.OccurredAt)
                    .FirstOrDefault(),
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
