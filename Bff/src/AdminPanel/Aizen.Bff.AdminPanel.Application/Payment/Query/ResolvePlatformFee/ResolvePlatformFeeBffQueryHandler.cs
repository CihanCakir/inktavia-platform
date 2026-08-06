using Aizen.Bff.AdminPanel.Application.Payment.Dto;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.Payment.Query.ResolvePlatformFee;

[DocumentationInfo("Resolve platform-fee BFF query handler (BE-P3)",
    "Point-in-time platform-fee resolution preview (GET /platform-fee/resolve). Read-only.")]
public sealed class ResolvePlatformFeeBffQueryHandler
    : AizenQueryHandler<ResolvePlatformFeeBffQuery, ResolvePlatformFeeBffResponse>
{
    private readonly IPaymentRemoteCall _payment;
    public ResolvePlatformFeeBffQueryHandler(IPaymentRemoteCall payment) => _payment = payment;

    public override async Task<ResolvePlatformFeeBffResponse?> Handle(ResolvePlatformFeeBffQuery request, CancellationToken ct)
        => new() { Result = await _payment.ResolvePlatformFeeAsync(
            request.CurrencyCode, request.CategoryCode, request.CustomerType, request.CustomerPayableServiceAmount, ct) };
}
