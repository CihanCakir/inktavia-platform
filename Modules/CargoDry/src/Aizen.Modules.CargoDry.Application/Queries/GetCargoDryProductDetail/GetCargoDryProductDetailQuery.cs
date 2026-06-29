using Aizen.Core.CQRS.Message;
using Aizen.Modules.CargoDry.Abstraction.Dto;

namespace Aizen.Modules.CargoDry.Application.Queries.GetCargoDryProductDetail;

public sealed class GetCargoDryProductDetailQuery : AizenQuery<CargoDryProductDto?>
{
    public string ProductCode { get; init; } = default!;
}
