using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.AdminServiceRequests.PricingAttributes;

/// <summary>S2a — envelope-correct result of a deactivate (the module soft-deletes; the code is kept).</summary>
public sealed class DeletePricingAttributeResult
{
    public bool Success { get; set; } = true;
}

/// <summary>S2a — deactivate a pricing attribute definition by id.</summary>
public sealed class DeletePricingAttributeCommand : AizenCommand<DeletePricingAttributeResult>
{
    public long Id { get; init; }
}

public sealed class DeletePricingAttributeCommandHandler
    : AizenCommandHandler<DeletePricingAttributeCommand, DeletePricingAttributeResult>
{
    private readonly IServiceRequestRemoteCall _serviceRequest;
    public DeletePricingAttributeCommandHandler(IServiceRequestRemoteCall serviceRequest)
        => _serviceRequest = serviceRequest;

    public override async Task<DeletePricingAttributeResult?> Handle(
        DeletePricingAttributeCommand request, CancellationToken ct)
    {
        await _serviceRequest.DeleteAdminPricingAttribute(request.Id);
        return new DeletePricingAttributeResult { Success = true };
    }
}
