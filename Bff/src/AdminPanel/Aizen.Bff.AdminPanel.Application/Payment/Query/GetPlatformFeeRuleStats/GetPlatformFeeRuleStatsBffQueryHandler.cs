using Aizen.Bff.AdminPanel.Application.Payment.Dto;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.Payment.Query.GetPlatformFeeRuleStats;

[DocumentationInfo("Get platform-fee rule stats BFF query handler (BE-P3)",
    "Returns KPI counts (total / active / per-model) for the admin platform-fee rules dashboard strip. Read-only.")]
public sealed class GetPlatformFeeRuleStatsBffQueryHandler
    : AizenQueryHandler<GetPlatformFeeRuleStatsBffQuery, GetPlatformFeeRuleStatsBffResponse>
{
    private readonly IPaymentRemoteCall _payment;
    public GetPlatformFeeRuleStatsBffQueryHandler(IPaymentRemoteCall payment) => _payment = payment;

    public override async Task<GetPlatformFeeRuleStatsBffResponse?> Handle(GetPlatformFeeRuleStatsBffQuery request, CancellationToken ct)
        => new() { Stats = await _payment.GetPlatformFeeRuleStatsAsync(ct) };
}
