using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;
using Aizen.Modules.ServiceRequest.Abstraction.Dto;

namespace Aizen.Bff.AdminPanel.Application.CargoDry.Query.GetCargoDrySupplyOrdersBff;

/// <summary>F1 — admin cargo-orders fulfilment queue. Proxies the ServiceRequest admin cargo-orders listing.</summary>
public sealed class GetCargoDrySupplyOrdersBffQuery : AizenQuery<CargoDrySupplyOrderAdminListDto>
{
    public string? Status   { get; init; }
    public int     Page     { get; init; } = 1;
    public int     PageSize { get; init; } = 25;
}

[DocumentationInfo("Get CargoDry cargo orders (BFF)",
    "Proxies the ServiceRequest admin cargo-orders listing (AwaitingShipment | Shipped | All) for the admin fulfilment queue.")]
public sealed class GetCargoDrySupplyOrdersBffQueryHandler
    : AizenQueryHandler<GetCargoDrySupplyOrdersBffQuery, CargoDrySupplyOrderAdminListDto>
{
    private readonly IServiceRequestRemoteCall _sr;
    public GetCargoDrySupplyOrdersBffQueryHandler(IServiceRequestRemoteCall sr) => _sr = sr;

    public override async Task<CargoDrySupplyOrderAdminListDto?> Handle(
        GetCargoDrySupplyOrdersBffQuery request, CancellationToken ct)
    {
        var result = await _sr.GetCargoDrySupplyOrders(request.Status, request.Page, request.PageSize);
        return result.Body ?? new CargoDrySupplyOrderAdminListDto { Page = request.Page, PageSize = request.PageSize };
    }
}
