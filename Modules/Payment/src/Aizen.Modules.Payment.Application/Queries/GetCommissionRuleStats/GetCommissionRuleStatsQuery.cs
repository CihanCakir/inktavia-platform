using Aizen.Core.CQRS.Message;
using Aizen.Modules.Payment.Application.Dto;

namespace Aizen.Modules.Payment.Application.Queries.GetCommissionRuleStats;

public sealed class GetCommissionRuleStatsQuery : AizenQuery<CommissionRuleStatsDto>;
