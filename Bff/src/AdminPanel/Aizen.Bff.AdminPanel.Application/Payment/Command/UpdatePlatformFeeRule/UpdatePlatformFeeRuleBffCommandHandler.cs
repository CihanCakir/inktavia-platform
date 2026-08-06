using Aizen.Bff.AdminPanel.Application.Payment.Dto;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.Payment.Command.UpdatePlatformFeeRule;

[DocumentationInfo("Update platform-fee rule BFF command handler (BE-P3)",
    "Forwards a platform-fee rule update (PUT /platform-fee/rules/{id}). Conflict/Invalid surfaces through the envelope.")]
public sealed class UpdatePlatformFeeRuleBffCommandHandler
    : AizenCommandHandler<UpdatePlatformFeeRuleBffCommand, UpdatePlatformFeeRuleBffResponse>
{
    private readonly IPaymentRemoteCall _payment;
    public UpdatePlatformFeeRuleBffCommandHandler(IPaymentRemoteCall payment) => _payment = payment;

    public override async Task<UpdatePlatformFeeRuleBffResponse?> Handle(UpdatePlatformFeeRuleBffCommand request, CancellationToken ct)
        => new() { Result = await _payment.UpdatePlatformFeeRuleAsync(request.Id, request.Body with { Id = request.Id }, ct) };
}
