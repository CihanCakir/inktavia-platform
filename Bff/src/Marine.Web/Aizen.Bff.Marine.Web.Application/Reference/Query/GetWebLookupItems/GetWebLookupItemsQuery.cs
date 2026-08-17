using Aizen.Core.CQRS.Message;
using Aizen.Modules.ReferenceData.Abstraction.Dto.LookupItem;

namespace Aizen.Bff.Marine.Web.Application.Reference.Query.GetWebLookupItems;

/// <summary>
/// GET /api/v1/web/reference/lookups/{groupCode} — active lookup options for a group (e.g. VESSEL_TYPE,
/// SERVICE_PROVIDER_CATEGORY) for the public website. The module DTO (<see cref="LookupItemDto"/>) is clean
/// reference data, so it is passed through unchanged.
/// </summary>
public sealed class GetWebLookupItemsQuery : AizenQuery<List<LookupItemDto>>
{
    public string GroupCode { get; set; } = default!;
}
