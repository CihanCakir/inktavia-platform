using Aizen.Modules.ServiceRequest.Abstraction.Dto;

namespace Aizen.Bff.MarineProvider.Application.ServiceRequests;

/// <summary>
/// Provider chat thread payload (items + channelOpen). BE_WC3d — the legacy SR-backed <c>GetProviderMessages</c> query
/// that used to co-define this type was retired; this shape is now produced only by the Messaging-backed
/// <c>GetProviderMessagingThread</c> (the unified provider thread read). Kept as a shared BFF DTO.
/// </summary>
public sealed class ProviderMessagesResponse
{
    public List<ServiceRequestMessageDto> Items { get; init; } = new();
    public bool ChannelOpen { get; init; }
    public int TotalCount { get; init; }
}
