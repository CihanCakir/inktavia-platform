using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Identity.Abstraction.Dto.Common;
using Microsoft.Extensions.Logging;

namespace Aizen.Bff.AdminPanel.Application.AdminPayment.Query.GetPaymentTransactionDetail;

[DocumentationInfo("Get payment transaction detail BFF query handler",
    "Returns a single payment transaction by ID. After fetching the transaction, resolves PayerDisplayName and " +
    "RecipientDisplayName from Identity in one bulk call — two IDs, one round-trip.")]
public sealed class GetPaymentTransactionDetailBffQueryHandler
    : AizenQueryHandler<GetPaymentTransactionDetailBffQuery, GetPaymentTransactionDetailBffResponse>
{
    private readonly IAdminPaymentBffRemoteCall  _payment;
    private readonly IIdentityAdminBffRemoteCall _identity;
    private readonly ILogger<GetPaymentTransactionDetailBffQueryHandler> _logger;

    public GetPaymentTransactionDetailBffQueryHandler(
        IAdminPaymentBffRemoteCall payment,
        IIdentityAdminBffRemoteCall identity,
        ILogger<GetPaymentTransactionDetailBffQueryHandler> logger)
    {
        _payment  = payment;
        _identity = identity;
        _logger   = logger;
    }

    public override async Task<GetPaymentTransactionDetailBffResponse> Handle(
        GetPaymentTransactionDetailBffQuery request, CancellationToken ct)
    {
        // 1. Fetch transaction — profile IDs are unknown until this resolves.
        var tx = await _payment.GetTransactionAsync(request.Id, ct);

        if (tx is null)
            return new GetPaymentTransactionDetailBffResponse { Transaction = null };

        // 2. Build the profile ID set (payer + optional recipient).
        var profileIds = new List<long> { tx.PayerProfileId };
        if (tx.RecipientProfileId.HasValue)
            profileIds.Add(tx.RecipientProfileId.Value);

        // 3. Single Identity bulk call to resolve both names at once.
        Dictionary<long, UserProfileListItemDto> profileMap = new();
        try
        {
            var identityResult = await _identity.GetUserProfilesByProfileIds(profileIds.ToArray());

            if (identityResult?.Header?.IsSuccess == true && identityResult.Body is { Count: > 0 })
            {
                profileMap = identityResult.Body
                    .GroupBy(p => p.Id)
                    .ToDictionary(g => g.Key, g => g.First());
            }
            else
            {
                _logger.LogWarning(
                    "[PaymentTxDetailBff] Identity bulk call returned IsSuccess={Success} for tx {Id}.",
                    identityResult?.Header?.IsSuccess, request.Id);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[PaymentTxDetailBff] Identity enrichment failed for tx {Id}.", request.Id);
        }

        // 4. Enrich the transaction DTO with resolved display names.
        var enriched = tx with
        {
            PayerDisplayName     = ResolveDisplayName(profileMap, tx.PayerProfileId),
            RecipientDisplayName = tx.RecipientProfileId.HasValue
                ? ResolveDisplayName(profileMap, tx.RecipientProfileId.Value)
                : null,
        };

        return new GetPaymentTransactionDetailBffResponse { Transaction = enriched };
    }

    private static string? ResolveDisplayName(Dictionary<long, UserProfileListItemDto> map, long profileId)
    {
        if (!map.TryGetValue(profileId, out var profile)) return null;
        var name = $"{profile.FirstName} {profile.LastName}".Trim();
        return name.Length > 0 ? name : null;
    }
}
