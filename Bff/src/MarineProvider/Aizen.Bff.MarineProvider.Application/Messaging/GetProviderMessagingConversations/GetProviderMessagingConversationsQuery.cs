using Aizen.Core.CQRS.Message;
using Aizen.Modules.ServiceRequest.Abstraction.Response.Message;

namespace Aizen.Bff.MarineProvider.Application.Messaging;

/// <summary>
/// Provider inbox (Phase 3) sourced from the unified MESSAGING module instead of the ServiceRequest module.
/// Returns the SAME <see cref="GetProviderConversationsResponse"/> shape the FE already consumes, so FE churn is a
/// URL swap only.
/// </summary>
public sealed class GetProviderMessagingConversationsQuery : AizenQuery<GetProviderConversationsResponse>
{
    public int Skip { get; init; }
    public int Take { get; init; } = 50;
}
