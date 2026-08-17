using Aizen.Bff.AdminPanel.Application.Payment.Dto;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Identity.Abstraction.Dto.Common;
using Microsoft.Extensions.Logging;

namespace Aizen.Bff.AdminPanel.Application.Payment.Query.GetPendingPayouts;

[DocumentationInfo("Get pending payouts BFF query handler",
    "Returns all provider payout records in Pending status. Batch-enriches ProviderDisplayName from " +
    "Identity module in a single bulk call after fetching the payout list.")]
public sealed class GetPendingPayoutsBffQueryHandler
    : AizenQueryHandler<GetPendingPayoutsBffQuery, GetPendingPayoutsBffResponse>
{
    private readonly IPaymentRemoteCall  _payment;
    private readonly IIdentityRemoteCall _identity;
    private readonly ILogger<GetPendingPayoutsBffQueryHandler> _logger;

    public GetPendingPayoutsBffQueryHandler(
        IPaymentRemoteCall payment,
        IIdentityRemoteCall identity,
        ILogger<GetPendingPayoutsBffQueryHandler> logger)
    {
        _payment  = payment;
        _identity = identity;
        _logger   = logger;
    }

    public override async Task<GetPendingPayoutsBffResponse> Handle(
        GetPendingPayoutsBffQuery request, CancellationToken ct)
    {
        // 1. Fetch pending payouts from Payment module.
        var payouts = await _payment.GetPendingPayoutsAsync(ct);

        if (payouts is not { Count: > 0 })
            return new GetPendingPayoutsBffResponse { Payouts = payouts ?? [] };

        // 2. Collect unique ProviderProfileIds across all pending payouts.
        var profileIds = payouts
            .Select(p => p.ProviderProfileId)
            .Distinct()
            .ToArray();

        // 3. Single bulk Identity call — one round-trip for all providers on the list.
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
                    "[PendingPayoutsBff] Identity bulk call returned IsSuccess={Success} for {Count} provider ids.",
                    identityResult?.Header?.IsSuccess, profileIds.Length);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[PendingPayoutsBff] Identity enrichment failed; returning unenriched data.");
        }

        // 4. Enrich each payout with provider display name and avatar.
        var enriched = payouts.Select(p =>
        {
            if (!profileMap.TryGetValue(p.ProviderProfileId, out var profile))
                return p;

            return p with
            {
                ProviderDisplayName = BuildDisplayName(profile),
                ProviderAvatarUrl   = profile.ProfilePhotoUrl,
            };
        }).ToList();

        return new GetPendingPayoutsBffResponse { Payouts = enriched };
    }

    private static string? BuildDisplayName(UserProfileListItemDto profile)
    {
        var name = $"{profile.FirstName} {profile.LastName}".Trim();
        return name.Length > 0 ? name : null;
    }
}
