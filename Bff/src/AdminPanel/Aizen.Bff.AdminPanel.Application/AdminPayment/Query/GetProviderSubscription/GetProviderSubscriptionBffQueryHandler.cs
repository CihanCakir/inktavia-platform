using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Identity.Abstraction.Dto.Common;
using Microsoft.Extensions.Logging;

namespace Aizen.Bff.AdminPanel.Application.AdminPayment.Query.GetProviderSubscription;

[DocumentationInfo("Get provider subscription BFF query handler",
    "Returns the active subscription record for a given provider profile. Since the ProviderProfileId is " +
    "known upfront, the Payment and Identity calls are fired in parallel via Task.WhenAll for optimal latency.")]
public sealed class GetProviderSubscriptionBffQueryHandler
    : AizenQueryHandler<GetProviderSubscriptionBffQuery, GetProviderSubscriptionBffResponse>
{
    private readonly IPaymentRemoteCall  _payment;
    private readonly IIdentityRemoteCall _identity;
    private readonly ILogger<GetProviderSubscriptionBffQueryHandler> _logger;

    public GetProviderSubscriptionBffQueryHandler(
        IPaymentRemoteCall payment,
        IIdentityRemoteCall identity,
        ILogger<GetProviderSubscriptionBffQueryHandler> logger)
    {
        _payment  = payment;
        _identity = identity;
        _logger   = logger;
    }

    public override async Task<GetProviderSubscriptionBffResponse> Handle(
        GetProviderSubscriptionBffQuery request, CancellationToken ct)
    {
        // Both calls can run in parallel — ProviderProfileId is known from the query.
        var subscriptionTask = _payment.GetProviderSubscriptionAsync(request.ProviderProfileId, ct);
        var identityTask     = _identity.GetUserProfilesByProfileIds(new[] { request.ProviderProfileId });

        await Task.WhenAll(subscriptionTask, identityTask);

        var subscription  = await subscriptionTask;

        UserProfileListItemDto? profile = null;
        try
        {
            var identityResult = await identityTask;
            if (identityResult?.Header?.IsSuccess == true)
                profile = identityResult.Body?.FirstOrDefault(p => p.Id == request.ProviderProfileId);
            else
                _logger.LogWarning(
                    "[ProviderSubBff] Identity call returned IsSuccess={Success} for provider {Id}.",
                    identityResult?.Header?.IsSuccess, request.ProviderProfileId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[ProviderSubBff] Identity enrichment failed for provider {Id}.",
                request.ProviderProfileId);
        }

        return new GetProviderSubscriptionBffResponse
        {
            Subscription        = subscription,
            ProviderDisplayName = BuildDisplayName(profile),
            ProviderAvatarUrl   = profile?.ProfilePhotoUrl,
        };
    }

    private static string? BuildDisplayName(UserProfileListItemDto? profile)
    {
        if (profile is null) return null;
        var name = $"{profile.FirstName} {profile.LastName}".Trim();
        return name.Length > 0 ? name : null;
    }
}
