using Aizen.Bff.MarineProvider.Application.Contracts.Me;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.MarineProvider.Application.Me.GetProviderProfile;

/// <summary>
/// Provider-safe view of the authenticated provider's own profile.
/// Admin-only fields (internal note, reviewer, rejection category, risk signals/level) are NOT exposed.
/// </summary>
public sealed class GetProviderProfileQuery : AizenQuery<GetProviderProfileResponse>
{
}
