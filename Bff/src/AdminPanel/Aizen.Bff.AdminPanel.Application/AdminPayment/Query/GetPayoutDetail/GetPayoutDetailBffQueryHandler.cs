using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Identity.Abstraction.Dto.Common;
using Microsoft.Extensions.Logging;

namespace Aizen.Bff.AdminPanel.Application.AdminPayment.Query.GetPayoutDetail;

[DocumentationInfo("Get payout detail BFF query handler",
    "Returns the full provider payout record by ID. After fetching the payout, resolves ProviderName " +
    "from Identity module in a follow-up call using the payout's ProviderProfileId.")]
public sealed class GetPayoutDetailBffQueryHandler
    : AizenQueryHandler<GetPayoutDetailBffQuery, GetPayoutDetailBffResponse>
{
    private readonly IAdminPaymentBffRemoteCall  _payment;
    private readonly IIdentityAdminBffRemoteCall _identity;
    private readonly ILogger<GetPayoutDetailBffQueryHandler> _logger;

    public GetPayoutDetailBffQueryHandler(
        IAdminPaymentBffRemoteCall payment,
        IIdentityAdminBffRemoteCall identity,
        ILogger<GetPayoutDetailBffQueryHandler> logger)
    {
        _payment  = payment;
        _identity = identity;
        _logger   = logger;
    }

    public override async Task<GetPayoutDetailBffResponse> Handle(
        GetPayoutDetailBffQuery request, CancellationToken ct)
    {
        // 1. Fetch payout — ProviderProfileId is unknown until this resolves.
        var payout = await _payment.GetPayoutDetailAsync(request.Id, ct);

        if (payout is null)
            return new GetPayoutDetailBffResponse { Payout = null };

        // 2. Resolve provider name from Identity.
        UserProfileListItemDto? profile = null;
        try
        {
            var identityResult = await _identity.GetUserProfilesByProfileIds(
                new[] { payout.ProviderProfileId });

            if (identityResult?.Header?.IsSuccess == true)
                profile = identityResult.Body?.FirstOrDefault(p => p.Id == payout.ProviderProfileId);
            else
                _logger.LogWarning(
                    "[PayoutDetailBff] Identity call returned IsSuccess={Success} for provider {Id}.",
                    identityResult?.Header?.IsSuccess, payout.ProviderProfileId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[PayoutDetailBff] Identity enrichment failed for payout {Id}.", request.Id);
        }

        var enriched = profile is null ? payout : payout with
        {
            ProviderName = BuildDisplayName(profile),
        };

        return new GetPayoutDetailBffResponse { Payout = enriched };
    }

    private static string? BuildDisplayName(UserProfileListItemDto profile)
    {
        var name = $"{profile.FirstName} {profile.LastName}".Trim();
        return name.Length > 0 ? name : null;
    }
}
