using Aizen.Core.CQRS.Message;
using Aizen.Modules.CargoDry.Abstraction.Dto;

namespace Aizen.Modules.CargoDry.Application.Queries.GetCargoDryBatchByCode;

public sealed class GetCargoDryBatchByCodeQuery : AizenQuery<CargoDryBatchDto?>
{
    public string BatchCode { get; init; } = default!;
}
