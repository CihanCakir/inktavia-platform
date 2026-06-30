using Aizen.Bff.AdminPanel.Application.AdminPayment.Dto;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Identity.Abstraction.Dto.Common;
using Microsoft.Extensions.Logging;

namespace Aizen.Bff.AdminPanel.Application.AdminPayment.Query.GetPayoutList;

[DocumentationInfo("Get payout list BFF query handler",
    "Returns paged provider payout records across all statuses. Batch-enriches ProviderName in " +
    "PaymentPayoutBffDto from Identity module in a single bulk call — one round-trip for the page.")]
public sealed class GetPayoutListBffQueryHandler
    : AizenQueryHandler<GetPayoutListBffQuery, GetPayoutListBffResponse>
{
    private readonly IAdminPaymentBffRemoteCall  _payment;
    private readonly IIdentityAdminBffRemoteCall _identity;
    private readonly ILogger<GetPayoutListBffQueryHandler> _logger;

    public GetPayoutListBffQueryHandler(
        IAdminPaymentBffRemoteCall payment,
        IIdentityAdminBffRemoteCall identity,
        ILogger<GetPayoutListBffQueryHandler> logger)
    {
        _payment  = payment;
        _identity = identity;
        _logger   = logger;
    }

    public override async Task<GetPayoutListBffResponse> Handle(
        GetPayoutListBffQuery request, CancellationToken ct)
    {
        // 1. Fetch paged payouts from Payment module.
        var result = await _payment.GetPayoutsPagedAsync(request.Status, request.Page, request.PageSize, ct);

        if (result?.Items is not { Count: > 0 })
            return new GetPayoutListBffResponse { Result = result! };

        // 2. Collect unique ProviderProfileIds for the page.
        var profileIds = result.Items
            .Select(p => p.ProviderProfileId)
            .Distinct()
            .ToArray();

        // 3. Single bulk Identity call.
        Dictionary<long, UserProfileListItemDto> profileMap = new();
        try
        {
            var identityResult = await _identity.GetUserProfilesByProfileIds(profileIds);

            if (identityResult?.Header?.IsSuccess == true && identityResult.Body is { Count: > 0 })
            {
                profileMap = identityResult.Body
                    .GroupBy(p => p.Id)
                    .ToDictionary(g => g.Key, g => g.First());
            }
            else
            {
                _logger.LogWarning(
                    "[PayoutListBff] Identity bulk call returned IsSuccess={Success} for {Count} provider ids.",
                    identityResult?.Header?.IsSuccess, profileIds.Length);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[PayoutListBff] Identity enrichment failed; returning unenriched data.");
        }

        // 4. Enrich ProviderName on each payout item.
        var enrichedItems = result.Items.Select(p =>
        {
            if (!profileMap.TryGetValue(p.ProviderProfileId, out var profile))
                return p;

            return p with { ProviderName = BuildDisplayName(profile) };
        }).ToList();

        return new GetPayoutListBffResponse
        {
            Result = new PaymentPayoutListBffResult(
                enrichedItems,
                result.Total,
                result.Page,
                result.PageSize)
        };
    }

    private static string? BuildDisplayName(UserProfileListItemDto profile)
    {
        var name = $"{profile.FirstName} {profile.LastName}".Trim();
        return name.Length > 0 ? name : null;
    }
}
