using Aizen.Core.CQRS.Message;
using Aizen.Modules.CargoDry.Abstraction.Dto;

namespace Aizen.Modules.CargoDry.Application.Queries.GetCargoDryWarehouseOptions;

public sealed class GetCargoDryWarehouseOptionsQuery : AizenQuery<List<CargoDryWarehouseOptionDto>>
{
}
