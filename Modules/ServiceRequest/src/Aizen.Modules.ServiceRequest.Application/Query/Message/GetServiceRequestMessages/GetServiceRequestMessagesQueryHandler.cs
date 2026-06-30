using Aizen.Core.CQRS.Handler;
using Aizen.Modules.ServiceRequest.Abstraction.Response.Message;
using Aizen.Modules.ServiceRequest.Domain.Interface.Repository;
using Aizen.Modules.ServiceRequest.Repository.Mapping;

namespace Aizen.Modules.ServiceRequest.Application.Query.Message;

[DocumentationInfo("Get messages query handler", "Fetches paginated messages for a service request.")]
public sealed class GetServiceRequestMessagesQueryHandler : AizenQueryHandler<GetServiceRequestMessagesQuery, GetServiceRequestMessagesResponse>
{
    private readonly IServiceRequestMessageRepository _repository;

    public GetServiceRequestMessagesQueryHandler(IServiceRequestMessageRepository repository) => _repository = repository;

    public override async Task<GetServiceRequestMessagesResponse> Handle(GetServiceRequestMessagesQuery request, CancellationToken cancellationToken)
    {
        var messages = await _repository.GetByServiceRequestIdAsync(request.ServiceRequestId, request.Skip, request.Take, cancellationToken);
        var dtos = messages.Select(m => m.ToDto()).ToList();
        return new GetServiceRequestMessagesResponse(dtos, dtos.Count);
    }
}
