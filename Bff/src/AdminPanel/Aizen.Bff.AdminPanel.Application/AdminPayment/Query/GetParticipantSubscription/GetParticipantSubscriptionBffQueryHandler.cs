using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Identity.Abstraction.Dto.Common;
using Microsoft.Extensions.Logging;

namespace Aizen.Bff.AdminPanel.Application.AdminPayment.Query.GetParticipantSubscription;

[DocumentationInfo("Get participant subscription BFF query handler",
    "Returns the active subscription record for a given participant profile. Since the ParticipantProfileId " +
    "is known upfront, the Payment and Identity calls are fired in parallel via Task.WhenAll for optimal latency.")]
public sealed class GetParticipantSubscriptionBffQueryHandler
    : AizenQueryHandler<GetParticipantSubscriptionBffQuery, GetParticipantSubscriptionBffResponse>
{
    private readonly IAdminPaymentBffRemoteCall  _payment;
    private readonly IIdentityAdminBffRemoteCall _identity;
    private readonly ILogger<GetParticipantSubscriptionBffQueryHandler> _logger;

    public GetParticipantSubscriptionBffQueryHandler(
        IAdminPaymentBffRemoteCall payment,
        IIdentityAdminBffRemoteCall identity,
        ILogger<GetParticipantSubscriptionBffQueryHandler> logger)
    {
        _payment  = payment;
        _identity = identity;
        _logger   = logger;
    }

    public override async Task<GetParticipantSubscriptionBffResponse> Handle(
        GetParticipantSubscriptionBffQuery request, CancellationToken ct)
    {
        // Both calls can run in parallel — ParticipantProfileId is known from the query.
        var subscriptionTask = _payment.GetParticipantSubscriptionAsync(request.ParticipantProfileId, ct);
        var identityTask     = _identity.GetUserProfilesByProfileIds(new[] { request.ParticipantProfileId });

        await Task.WhenAll(subscriptionTask, identityTask);

        var subscription = await subscriptionTask;

        UserProfileListItemDto? profile = null;
        try
        {
            var identityResult = await identityTask;
            if (identityResult?.Header?.IsSuccess == true)
                profile = identityResult.Body?.FirstOrDefault(p => p.Id == request.ParticipantProfileId);
            else
                _logger.LogWarning(
                    "[ParticipantSubBff] Identity call returned IsSuccess={Success} for participant {Id}.",
                    identityResult?.Header?.IsSuccess, request.ParticipantProfileId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[ParticipantSubBff] Identity enrichment failed for participant {Id}.",
                request.ParticipantProfileId);
        }

        return new GetParticipantSubscriptionBffResponse
        {
            Subscription           = subscription,
            ParticipantDisplayName = BuildDisplayName(profile),
            ParticipantAvatarUrl   = profile?.ProfilePhotoUrl,
        };
    }

    private static string? BuildDisplayName(UserProfileListItemDto? profile)
    {
        if (profile is null) return null;
        var name = $"{profile.FirstName} {profile.LastName}".Trim();
        return name.Length > 0 ? name : null;
    }
}
