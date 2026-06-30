using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;

namespace Aizen.Bff.AdminPanel.Application.AdminPayment.Command.CreatePaymentEscrow;

[DocumentationInfo("Create payment escrow BFF command handler",
    "Creates an escrow transaction for a service request offer acceptance. Used by the admin to initiate held payment flows.")]
public sealed class CreatePaymentEscrowBffCommandHandler
    : AizenCommandHandler<CreatePaymentEscrowBffCommand, CreatePaymentEscrowBffCommandResponse>
{
    private readonly IAdminPaymentBffRemoteCall _remote;

    public CreatePaymentEscrowBffCommandHandler(IAdminPaymentBffRemoteCall remote)
        => _remote = remote;

    public override async Task<CreatePaymentEscrowBffCommandResponse?> Handle(
        CreatePaymentEscrowBffCommand request, CancellationToken ct)
    {
        var result = await _remote.CreateEscrowAsync(request.Body, ct);
        return new CreatePaymentEscrowBffCommandResponse { Result = result };
    }
}
