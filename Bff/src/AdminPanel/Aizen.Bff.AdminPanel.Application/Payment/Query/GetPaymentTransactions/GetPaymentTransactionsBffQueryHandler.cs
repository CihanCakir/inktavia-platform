using Aizen.Bff.AdminPanel.Application.Payment.Dto;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Identity.Abstraction.Dto.Common;
using Microsoft.Extensions.Logging;

namespace Aizen.Bff.AdminPanel.Application.Payment.Query.GetPaymentTransactions;

[DocumentationInfo("Get payment transactions BFF query handler",
    "Returns paged payment transaction list. Batch-enriches PayerDisplayName and RecipientDisplayName " +
    "from Identity in a single bulk call. ContextType + ContextId are passed through from the Payment " +
    "module so the frontend can render SR/CargoDry/Subscription badges without extra calls. " +
    "ServiceRequest code/title are NOT enriched in the list (would be N SR calls for N items); " +
    "use the detail endpoint for full SR context.")]
public sealed class GetPaymentTransactionsBffQueryHandler
    : AizenQueryHandler<GetPaymentTransactionsBffQuery, GetPaymentTransactionsBffResponse>
{
    private readonly IPaymentRemoteCall  _payment;
    private readonly IIdentityRemoteCall _identity;
    private readonly ILogger<GetPaymentTransactionsBffQueryHandler> _logger;

    public GetPaymentTransactionsBffQueryHandler(
        IPaymentRemoteCall payment,
        IIdentityRemoteCall identity,
        ILogger<GetPaymentTransactionsBffQueryHandler> logger)
    {
        _payment  = payment;
        _identity = identity;
        _logger   = logger;
    }

    public override async Task<GetPaymentTransactionsBffResponse> Handle(
        GetPaymentTransactionsBffQuery request, CancellationToken ct)
    {
        // 1. Fetch paged transaction list from Payment module.
        // ContextType + ContextId are already in the DTO from the Payment API.
        var result = await _payment.GetTransactionsAsync(
            request.Status, request.Type, request.Gateway,
            request.FromDate, request.ToDate, request.Search,
            request.Page, request.PageSize, ct);

        if (result?.Items is not { Count: > 0 })
            return new GetPaymentTransactionsBffResponse { Result = result! };

        // 2. Collect all unique profile IDs across the page (payer + optional recipient).
        var profileIds = result.Items
            .SelectMany(t => new long?[] { t.PayerProfileId, t.RecipientProfileId })
            .Where(id => id.HasValue)
            .Select(id => id!.Value)
            .Distinct()
            .ToArray();

        if (profileIds.Length == 0)
            return new GetPaymentTransactionsBffResponse { Result = result };

        // 3. Single bulk Identity call — one round-trip for the whole page.
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
                    "[PaymentTxListBff] Identity bulk-by-profile-ids returned IsSuccess={Success} for {Count} ids.",
                    identityResult?.Header?.IsSuccess, profileIds.Length);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[PaymentTxListBff] Identity enrichment failed; returning unenriched data.");
        }

        // 4. Enrich display names. ContextType/ContextId/ContextSubId pass through unchanged.
        var enrichedItems = result.Items.Select(t => t with
        {
            PayerDisplayName     = ResolveDisplayName(profileMap, t.PayerProfileId),
            RecipientDisplayName = t.RecipientProfileId.HasValue
                ? ResolveDisplayName(profileMap, t.RecipientProfileId.Value)
                : null,
        }).ToList();

        return new GetPaymentTransactionsBffResponse
        {
            Result = new PaymentTransactionListBffResult(
                enrichedItems,
                result.Total,
                result.Page,
                result.PageSize)
        };
    }

    private static string? ResolveDisplayName(Dictionary<long, UserProfileListItemDto> map, long profileId)
    {
        if (!map.TryGetValue(profileId, out var profile)) return null;
        var name = $"{profile.FirstName} {profile.LastName}".Trim();
        return name.Length > 0 ? name : null;
    }
}
