using Aizen.Bff.MarineProvider.Application.Contracts.Reference;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.MarineProvider.Application.Reference;

/// <summary>GET /api/v1/provider/reference/{groupCode} — lookup options for one group.</summary>
public sealed class GetReferenceItemsQuery : AizenQuery<List<ReferenceItemDto>>
{
    public string GroupCode { get; }
    public GetReferenceItemsQuery(string groupCode) => GroupCode = groupCode;
}
