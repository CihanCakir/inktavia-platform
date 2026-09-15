using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;
using Aizen.Modules.ServiceRequest.Abstraction.Dto;

namespace Aizen.Bff.AdminPanel.Application.CargoDry.Query.GetCargoDrySupplyOrdersBff;

/// <summary>Admin cargo-orders + history. Proxies the ServiceRequest admin cargo-orders listing
/// (all|awaiting|shipped|completed|cancelled); OwnerUserId narrows to one owner's cargo purchase history.</summary>
public sealed class GetCargoDrySupplyOrdersBffQuery : AizenQuery<CargoDrySupplyOrderAdminListDto>
{
    public string? Status      { get; init; }
    public long?   OwnerUserId { get; init; }
    public int     Page        { get; init; } = 1;
    public int     PageSize    { get; init; } = 25;
}

[DocumentationInfo("Get CargoDry cargo orders (BFF)",
    "Proxies the ServiceRequest admin cargo-orders listing (AwaitingShipment | Shipped | All) for the admin fulfilment queue, " +
    "enriching each row with the owner display name from Identity (best-effort).")]
public sealed class GetCargoDrySupplyOrdersBffQueryHandler
    : AizenQueryHandler<GetCargoDrySupplyOrdersBffQuery, CargoDrySupplyOrderAdminListDto>
{
    private readonly IServiceRequestRemoteCall _sr;
    private readonly IIdentityRemoteCall       _identity;

    public GetCargoDrySupplyOrdersBffQueryHandler(IServiceRequestRemoteCall sr, IIdentityRemoteCall identity)
    {
        _sr       = sr;
        _identity = identity;
    }

    public override async Task<CargoDrySupplyOrderAdminListDto?> Handle(
        GetCargoDrySupplyOrdersBffQuery request, CancellationToken ct)
    {
        var result = await _sr.GetCargoDrySupplyOrders(request.Status, request.OwnerUserId, request.Page, request.PageSize);
        var list = result.Body ?? new CargoDrySupplyOrderAdminListDto { Page = request.Page, PageSize = request.PageSize };

        await TryEnrichOwnerNamesAsync(list);
        return list;
    }

    // Best-effort owner-name enrichment (mirrors the vessel-list pattern). Identity being unavailable must never
    // fail the queue — rows just render without an owner name.
    private async Task TryEnrichOwnerNamesAsync(CargoDrySupplyOrderAdminListDto list)
    {
        var ownerUserIds = list.Items
            .Where(i => i.OwnerUserId > 0)
            .Select(i => i.OwnerUserId)
            .Distinct()
            .ToArray();

        if (ownerUserIds.Length == 0) return;

        try
        {
            var profileResult = await _identity.GetUserProfilesByUserIds(ownerUserIds);
            if (profileResult?.Header?.IsSuccess != true || profileResult.Body is null)
                return;

            var byUserId = profileResult.Body
                .Where(p => p.UserId > 0)
                .GroupBy(p => p.UserId)
                .ToDictionary(g => g.Key, g => g.First());

            foreach (var item in list.Items)
            {
                if (byUserId.TryGetValue(item.OwnerUserId, out var profile))
                {
                    var fullName = $"{profile.FirstName} {profile.LastName}".Trim();
                    item.OwnerName = string.IsNullOrWhiteSpace(fullName) ? null : fullName;
                }
            }
        }
        catch
        {
            // Identity unavailable — leave OwnerName null.
        }
    }
}
