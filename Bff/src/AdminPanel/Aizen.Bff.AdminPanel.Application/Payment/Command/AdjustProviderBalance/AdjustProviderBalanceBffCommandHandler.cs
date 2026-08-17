using Aizen.Bff.AdminPanel.Application.Payment.Dto;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;
using Aizen.Modules.Payment.Abstraction.Dto;

namespace Aizen.Bff.AdminPanel.Application.Payment.Command.AdjustProviderBalance;

[DocumentationInfo("Adjust provider balance BFF command handler (P10)",
    "Forwards a manual signed balance adjustment (POST /admin/provider-balances/{providerProfileId}/adjust).")]
public sealed class AdjustProviderBalanceBffCommandHandler
    : AizenCommandHandler<AdjustProviderBalanceBffCommand, AdjustProviderBalanceBffResponse>
{
    private readonly IPaymentRemoteCall _payment;
    public AdjustProviderBalanceBffCommandHandler(IPaymentRemoteCall payment) => _payment = payment;

    public override async Task<AdjustProviderBalanceBffResponse?> Handle(AdjustProviderBalanceBffCommand request, CancellationToken ct)
        => new() { Result = await _payment.AdjustProviderBalanceAsync(request.ProviderProfileId, request.Body, ct) };
}
