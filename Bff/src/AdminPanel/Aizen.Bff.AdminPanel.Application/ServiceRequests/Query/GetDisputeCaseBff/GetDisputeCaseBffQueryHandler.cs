using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Modules.ServiceRequest.Abstraction.Response.Dispute;

namespace Aizen.Bff.AdminPanel.Application.ServiceRequests.Query;

[DocumentationInfo("Get dispute case query handler", "Fetches the consolidated dispute case file via the ServiceRequest module (BE-S13a).")]
public sealed class GetDisputeCaseBffQueryHandler
    : AizenQueryHandler<GetDisputeCaseBffQuery, GetDisputeCaseDetailResponse>
{
    private readonly IServiceRequestRemoteCall _serviceRequest;

    public GetDisputeCaseBffQueryHandler(IServiceRequestRemoteCall serviceRequest)
    {
        _serviceRequest = serviceRequest;
    }

    public override async Task<GetDisputeCaseDetailResponse?> Handle(
        GetDisputeCaseBffQuery request, CancellationToken cancellationToken)
    {
        var result = await _serviceRequest.GetDisputeCase(request.ServiceRequestId, request.DisputeId);
        return result.Body;
    }
}
