using Aizen.Core.CQRS.Message;
using Aizen.Modules.CargoDry.Abstraction.Dto;
using Aizen.Modules.CargoDry.Abstraction.Enum;

namespace Aizen.Bff.MarineProvider.Application.CargoDry;

/// <summary>
/// CargoDry supply v2 — provider kit picker passthrough. Returns the calling provider's own kits
/// (optionally filtered by product + status). Provider identity is resolved server-side, never trusted from input.
/// </summary>
public sealed class GetCargoDryProviderKitsBffQuery : AizenQuery<List<CargoDryProviderKitDto>>
{
    public string?           ProductCode { get; init; }
    public CargoDryKitStatus? Status     { get; init; }
    public int               PageSize    { get; init; } = 100;
}
