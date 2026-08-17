using Aizen.Modules.CargoDry.Abstraction.Dto;

namespace Aizen.Modules.CargoDry.Application.Queries.GetMyKits;

public sealed class GetMyKitsResponse
{
    public List<CargoDryKitDto> Items         { get; init; } = [];
    public int                  Total         { get; init; }
    public int                  ActiveCount   { get; init; }
    public int                  ExpiringCount { get; init; }
}
