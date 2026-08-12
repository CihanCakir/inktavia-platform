using Aizen.Modules.Notification.Abstraction.RemoteCall;
using Microsoft.Extensions.Logging;

namespace Aizen.Modules.Notification.Consumers.ServiceRequest;

/// <summary>
/// BE_NF1b — resolves an owner's Identity USER id (<c>SR.OwnerUserId</c>) to their participant PROFILE id, so
/// owner-facing notifications are filed where the owner's mobile inbox + device tokens resolve (symmetric with the
/// provider, keyed by ProviderProfileId). Falls back to the user id (with a warning) if the resolver returns nothing —
/// the notification is never dropped, it just may not surface until the profile link exists.
/// </summary>
internal static class OwnerRecipientResolver
{
    public static async Task<long> ResolveAsync(
        INotificationIdentityRemoteCall identity, ILogger logger, long ownerUserId, CancellationToken ct)
    {
        if (ownerUserId <= 0)
            return ownerUserId;

        try
        {
            var response = await identity.GetParticipantProfileIdByUserId(ownerUserId);
            var profileId = response?.Body?.ProfileId ?? 0;
            if (profileId > 0)
                return profileId;

            logger.LogWarning(
                "BE_NF1b: no participant profile for owner user {OwnerUserId}; filing notification under the user id " +
                "(owner inbox may not surface it until the profile link exists).", ownerUserId);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex,
                "BE_NF1b: participant-profile resolve failed for owner user {OwnerUserId}; filing under the user id.",
                ownerUserId);
        }

        return ownerUserId;
    }
}
