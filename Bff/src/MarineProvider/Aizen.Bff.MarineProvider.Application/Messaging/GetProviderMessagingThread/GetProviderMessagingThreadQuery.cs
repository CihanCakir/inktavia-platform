using Aizen.Bff.MarineProvider.Application.ServiceRequests;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.MarineProvider.Application.Messaging;

/// <summary>
/// A single provider thread (Phase 3), keyed by <see cref="ServiceRequestId"/> and sourced from the unified
/// MESSAGING module. Returns the SAME <see cref="ProviderMessagesResponse"/> shape the FE thread already consumes.
/// </summary>
public sealed class GetProviderMessagingThreadQuery : AizenQuery<ProviderMessagesResponse>
{
    public long ServiceRequestId { get; init; }
}
