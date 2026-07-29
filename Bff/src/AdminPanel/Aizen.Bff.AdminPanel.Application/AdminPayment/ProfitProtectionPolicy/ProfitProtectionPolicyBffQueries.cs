using Aizen.Bff.AdminPanel.Application.AdminPayment.Dto;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.AdminPayment.ProfitProtectionPolicy;

// ─── Resolve (active policy preview) ─────────────────────────────────────────
public sealed class ResolveProfitProtectionPolicyBffQuery : AizenQuery<ResolveProfitProtectionPolicyBffResponse>
{
    public string    CurrencyCode { get; init; } = "TRY";
    public DateTime? AtUtc        { get; init; }
}
public sealed class ResolveProfitProtectionPolicyBffResponse { public ProfitProtectionPolicyResolveBffResult? Result { get; init; } }

[DocumentationInfo("Resolve profit-protection policy BFF query handler (BE-P5)",
    "Resolves the single active policy for a currency at an instant (GET /profit-protection/resolve). Read-only.")]
public sealed class ResolveProfitProtectionPolicyBffQueryHandler
    : AizenQueryHandler<ResolveProfitProtectionPolicyBffQuery, ResolveProfitProtectionPolicyBffResponse>
{
    private readonly IAdminPaymentBffRemoteCall _payment;
    public ResolveProfitProtectionPolicyBffQueryHandler(IAdminPaymentBffRemoteCall payment) => _payment = payment;

    public override async Task<ResolveProfitProtectionPolicyBffResponse?> Handle(ResolveProfitProtectionPolicyBffQuery request, CancellationToken ct)
        => new() { Result = await _payment.ResolveProfitProtectionPolicyAsync(request.CurrencyCode, request.AtUtc, ct) };
}
