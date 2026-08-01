using Aizen.Core.CQRS.Message;
using Aizen.Modules.Payment.Application.Dto;

namespace Aizen.Modules.Payment.Application.Queries.GetPlatformFeeRuleStats;

public sealed class GetPlatformFeeRuleStatsQuery : AizenQuery<PlatformFeeRuleStatsDto>;
