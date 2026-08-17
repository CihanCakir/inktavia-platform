using Aizen.Bff.Marine.Participant.Mobile.Application.Contracts.Reference;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.Marine.Participant.Mobile.Application.Reference;

/// <summary>GET /api/v1/mobile/reference/{groupCode} — lookup options for one group.</summary>
public sealed class GetReferenceItemsQuery : AizenQuery<List<ReferenceItemDto>>
{
    public string GroupCode { get; }
    public GetReferenceItemsQuery(string groupCode) => GroupCode = groupCode;
}
