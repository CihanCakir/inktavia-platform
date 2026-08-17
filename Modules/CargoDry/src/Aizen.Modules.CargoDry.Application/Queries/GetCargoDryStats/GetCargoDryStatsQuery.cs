using Aizen.Core.CQRS.Message;
using Aizen.Modules.CargoDry.Abstraction.Dto;

namespace Aizen.Modules.CargoDry.Application.Queries.GetCargoDryStats;

public sealed class GetCargoDryStatsQuery : AizenQuery<CargoDryStatsDto> { }
