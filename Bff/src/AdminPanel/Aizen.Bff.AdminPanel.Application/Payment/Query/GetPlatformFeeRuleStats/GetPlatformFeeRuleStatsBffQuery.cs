using Aizen.Bff.AdminPanel.Application.Payment.Dto;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.Payment.Query.GetPlatformFeeRuleStats;


// ─── Stats (KPI strip) ───────────────────────────────────────────────────────
public sealed class GetPlatformFeeRuleStatsBffQuery : AizenQuery<GetPlatformFeeRuleStatsBffResponse>;
public sealed class GetPlatformFeeRuleStatsBffResponse { public PlatformFeeRuleStatsBffDto? Stats { get; init; } }
