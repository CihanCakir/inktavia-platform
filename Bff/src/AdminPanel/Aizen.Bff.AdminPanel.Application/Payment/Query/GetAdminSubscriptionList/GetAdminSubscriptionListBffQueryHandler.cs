using Aizen.Bff.AdminPanel.Application.Payment.Dto;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Microsoft.Extensions.Logging;

namespace Aizen.Bff.AdminPanel.Application.Payment.Query.GetAdminSubscriptionList;

[DocumentationInfo("GetAdminSubscriptionListBffQueryHandler",
    "Returns a paged, merged list of provider and participant subscriptions for admin oversight. " +
    "After fetching from the Payment module, bulk-resolves profile display names from the Identity " +
    "service using GetUserProfilesByProfileIds. " +
    "Supports filtering by Audience (Provider/Participant) and Status.")]
public sealed class GetAdminSubscriptionListBffQueryHandler
    : AizenQueryHandler<GetAdminSubscriptionListBffQuery, GetAdminSubscriptionListBffResponse>
{
    private readonly IPaymentRemoteCall  _remote;
    private readonly IIdentityRemoteCall _identity;
    private readonly ILogger<GetAdminSubscriptionListBffQueryHandler> _logger;

    public GetAdminSubscriptionListBffQueryHandler(
        IPaymentRemoteCall  remote,
        IIdentityRemoteCall identity,
        ILogger<GetAdminSubscriptionListBffQueryHandler> logger)
    {
        _remote   = remote;
        _identity = identity;
        _logger   = logger;
    }

    public override async Task<GetAdminSubscriptionListBffResponse> Handle(
        GetAdminSubscriptionListBffQuery request, CancellationToken ct)
    {
        // 1. Fetch raw subscription list from Payment module
        var raw = await _remote.GetAdminSubscriptionListAsync(
            audience: request.Audience,
            status:   request.Status,
            page:     request.Page,
            pageSize: request.PageSize,
            ct:       ct);

        // 2. Bulk-resolve profile names from Identity
        var nameMap = await ResolveProfileNamesAsync(raw.Items, ct);

        // 3. Enrich items
        var enriched = raw.Items
            .Select(item =>
            {
                nameMap.TryGetValue(item.ProfileId, out var profile);
                return new AdminSubscriptionListItemBffDto(
                    Id:                 item.Id,
                    Audience:           item.Audience,
                    ProfileId:          item.ProfileId,
                    UserId:             profile?.UserId   ?? 0,
                    ProfileDisplayName: profile != null
                        ? $"{profile.FirstName} {profile.LastName}".Trim()
                        : $"#{item.ProfileId}",
                    PlanId:       item.PlanId,
                    PlanCode:     item.PlanCode,
                    PlanName:     item.PlanName,
                    Status:       item.Status,
                    PaidAmount:   item.PaidAmount,
                    CurrencyCode: item.CurrencyCode,
                    PeriodStart:  item.PeriodStart,
                    PeriodEnd:    item.PeriodEnd,
                    AutoRenew:    item.AutoRenew,
                    CreateDate:   item.CreateDate
                );
            })
            .ToList();

        return new GetAdminSubscriptionListBffResponse
        {
            Result = new AdminSubscriptionListBffResult(
                Items:    enriched,
                Total:    raw.Total,
                Page:     raw.Page,
                PageSize: raw.PageSize)
        };
    }

    private async Task<Dictionary<long, IdentityProfile>> ResolveProfileNamesAsync(
        List<AdminSubscriptionListItemBffDto> items, CancellationToken ct)
    {
        var profileIds = items.Select(x => x.ProfileId).Distinct().ToArray();
        if (profileIds.Length == 0)
            return new Dictionary<long, IdentityProfile>();

        try
        {
            var response = await _identity.GetUserProfilesByProfileIds(profileIds);
            if (response?.Header?.IsSuccess != true || response.Body == null)
            {
                _logger.LogWarning("[SubListBff] Identity bulk profile resolve returned non-success.");
                return new Dictionary<long, IdentityProfile>();
            }

            return response.Body.ToDictionary(
                p => p.Id,
                p => new IdentityProfile(p.UserId, p.FirstName, p.LastName));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[SubListBff] Identity bulk profile resolve failed: {Message}", ex.Message);
            return new Dictionary<long, IdentityProfile>();
        }
    }

    private sealed record IdentityProfile(long UserId, string FirstName, string LastName);
}
