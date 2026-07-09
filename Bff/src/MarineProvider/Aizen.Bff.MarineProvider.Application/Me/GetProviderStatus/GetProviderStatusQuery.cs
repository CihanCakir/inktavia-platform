using Aizen.Bff.MarineProvider.Application.Contracts.Me;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.MarineProvider.Application.Me.GetProviderStatus;

/// <summary>
/// Account status gate. Works for pending/non-active providers. Approval/profile status is read from
/// Identity at runtime; token roles are not trusted for the decision. Admin-only fields are never exposed.
/// </summary>
public sealed class GetProviderStatusQuery : AizenQuery<GetProviderStatusResponse>
{
}
