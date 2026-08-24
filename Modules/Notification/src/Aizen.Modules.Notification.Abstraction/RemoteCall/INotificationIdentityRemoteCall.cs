using Aizen.Core.Infrastructure.Api;
using Aizen.Core.RemoteCall.Abstraction;

namespace Aizen.Modules.Notification.Abstraction.RemoteCall;

/// <summary>
/// N-C — resolves region-eligible providers from the Identity I2 read-model (GetProvidersForArea). Internal
/// module-to-module read (Identity endpoint is anonymous behind the cluster NetworkPolicy — same pattern as
/// ServiceRequest→ReferenceData). Auto-registered by the Core remote-call scan; base URL from
/// RemoteCalls__INotificationIdentityRemoteCall__BaseUrl.
/// </summary>
public interface INotificationIdentityRemoteCall : IAizenRemoteCall
{
    [AizenRemoteCallGet("/api/v1/identity/providers/for-area")]
    Task<AizenApiResponse<List<ProviderForAreaResult>>> GetProvidersForArea(
        [Refit.Query] string cityCode,
        [Refit.Query] string? categoryCode = null,
        [Refit.Query] int take = 500);

    // N-D — admin user ids for the support-request fan-out.
    [AizenRemoteCallGet("/api/v1/identity/admin/user-ids")]
    Task<AizenApiResponse<List<long>>> GetAdminUserIds();

    // BE_NF1b — resolve a participant USER id to its participant PROFILE id, so owner-facing notifications are filed
    // under the profile id (where the owner mobile inbox + device tokens resolve). ProfileId=0 when none.
    [AizenRemoteCallGet("/api/v1/identity/participant/profile-id")]
    Task<AizenApiResponse<ParticipantProfileIdResult>> GetParticipantProfileIdByUserId([Refit.Query] long userId);

    // BE_NF2 — resolve a profile's contact email by UserProfiles.Id (participant or provider profile), to address an
    // Email-channel delivery to the same recipient the InApp notification is filed under. Email is null when none.
    [AizenRemoteCallGet("/api/v1/identity/profiles/contact-email")]
    Task<AizenApiResponse<ProfileContactEmailResult>> GetProfileContactEmail([Refit.Query] long profileId);

    // Locale — resolve a profile's persisted preferred language by UserProfiles.Id, using the SAME UserProfiles→Users
    // lookup as the contact-email read. PreferredLanguage is null when the profile/user has none set.
    [AizenRemoteCallGet("/api/v1/identity/profiles/preferred-language")]
    Task<AizenApiResponse<ProfilePreferredLanguageResult>> GetProfilePreferredLanguage([Refit.Query] long profileId);
}

/// <summary>BE_NF2 — module-local mirror of Identity's ProfileContactEmailDto (deserialized by JSON property name).</summary>
public sealed class ProfileContactEmailResult
{
    public long ProfileId { get; set; }
    public string? Email { get; set; }
}

/// <summary>Locale — module-local mirror of Identity's ProfilePreferredLanguageDto (deserialized by JSON property name).</summary>
public sealed class ProfilePreferredLanguageResult
{
    public long ProfileId { get; set; }
    public string? PreferredLanguage { get; set; }
}

/// <summary>BE_NF1b — module-local mirror of Identity's ParticipantProfileIdDto (deserialized by JSON property name).</summary>
public sealed class ParticipantProfileIdResult
{
    public long UserId { get; set; }
    public long ProfileId { get; set; }
}

/// <summary>BFF/module-local mirror of Identity's ProviderForAreaDto (deserialized by JSON property name).</summary>
public sealed class ProviderForAreaResult
{
    public long ProfileId { get; set; }
    public long UserId    { get; set; }
}
