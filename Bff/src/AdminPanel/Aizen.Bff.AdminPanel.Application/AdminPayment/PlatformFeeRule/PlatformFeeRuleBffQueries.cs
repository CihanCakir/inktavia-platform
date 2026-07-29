using Aizen.Bff.AdminPanel.Application.AdminPayment.Dto;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.AdminPayment.PlatformFeeRule;

// ─── Resolve (point-in-time preview) ─────────────────────────────────────────
public sealed class ResolvePlatformFeeBffQuery : AizenQuery<ResolvePlatformFeeBffResponse>
{
    public string   CurrencyCode                 { get; init; } = "TRY";
    public string?  CategoryCode                 { get; init; }
    public string?  CustomerType                 { get; init; }
    public decimal  CustomerPayableServiceAmount { get; init; }
}
public sealed class ResolvePlatformFeeBffResponse { public PlatformFeeResolveBffResult? Result { get; init; } }

[DocumentationInfo("Resolve platform-fee BFF query handler (BE-P3)",
    "Point-in-time platform-fee resolution preview (GET /platform-fee/resolve). Read-only.")]
public sealed class ResolvePlatformFeeBffQueryHandler
    : AizenQueryHandler<ResolvePlatformFeeBffQuery, ResolvePlatformFeeBffResponse>
{
    private readonly IAdminPaymentBffRemoteCall _payment;
    public ResolvePlatformFeeBffQueryHandler(IAdminPaymentBffRemoteCall payment) => _payment = payment;

    public override async Task<ResolvePlatformFeeBffResponse?> Handle(ResolvePlatformFeeBffQuery request, CancellationToken ct)
        => new() { Result = await _payment.ResolvePlatformFeeAsync(
            request.CurrencyCode, request.CategoryCode, request.CustomerType, request.CustomerPayableServiceAmount, ct) };
}
