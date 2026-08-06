using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.ServiceRequests.Command;


public sealed class DeletePricingAttributeBffCommandHandler
    : AizenCommandHandler<DeletePricingAttributeBffCommand, DeletePricingAttributeResult>
{
    private readonly IServiceRequestRemoteCall _serviceRequest;
    public DeletePricingAttributeBffCommandHandler(IServiceRequestRemoteCall serviceRequest)
        => _serviceRequest = serviceRequest;

    public override async Task<DeletePricingAttributeResult?> Handle(
        DeletePricingAttributeBffCommand request, CancellationToken ct)
    {
        await _serviceRequest.DeleteAdminPricingAttribute(request.Id);
        return new DeletePricingAttributeResult { Success = true };
    }
}
