using Aizen.Core.CQRS.Message;
using Aizen.Modules.CargoDry.Abstraction.Dto;
using Aizen.Modules.CargoDry.Abstraction.Enum;

namespace Aizen.Modules.CargoDry.Application.Queries.GetProviderKits;

/// <summary>
/// CargoDry supply v2 — provider kit picker. Returns the calling provider's own kits (resolved from the
/// token, never a query param), optionally filtered by product and status. Backs the provider-portal picker.
/// </summary>
public sealed class GetProviderKitsQuery : AizenQuery<List<CargoDryProviderKitDto>>
{
    public long              ProviderProfileId { get; init; }
    public string?           ProductCode       { get; init; }
    public CargoDryKitStatus? Status           { get; init; }
    public int               PageSize          { get; init; } = 100;
}
