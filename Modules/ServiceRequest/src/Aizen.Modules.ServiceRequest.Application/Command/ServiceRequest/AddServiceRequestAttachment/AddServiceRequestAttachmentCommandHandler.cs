using Aizen.Core.CQRS.Handler;
using Aizen.Core.InfoAccessor.Abstraction;
using Aizen.Modules.ServiceRequest.Abstraction.Enum;
using Aizen.Modules.ServiceRequest.Abstraction.Model;
using Aizen.Modules.ServiceRequest.Abstraction.Response.ServiceRequest;
using Aizen.Modules.ServiceRequest.Domain.Entities.ServiceRequest;
using Aizen.Modules.ServiceRequest.Domain.Interface.Repository;
using Aizen.Modules.ServiceRequest.Repository.Mapping;

namespace Aizen.Modules.ServiceRequest.Application.Command.ServiceRequest;

[DocumentationInfo("Add attachment command handler", "Adds a file attachment to the service request.")]
public sealed class AddServiceRequestAttachmentCommandHandler : AizenCommandHandler<AddServiceRequestAttachmentCommand, AddServiceRequestAttachmentResponse>
{
    private readonly IServiceRequestRepository _repository;
    private readonly IAizenInfoAccessor _info;

    public AddServiceRequestAttachmentCommandHandler(IServiceRequestRepository repository, IAizenInfoAccessor info)
    {
        _repository = repository; _info = info;
    }

    public override async Task<AddServiceRequestAttachmentResponse?> Handle(AddServiceRequestAttachmentCommand request, CancellationToken cancellationToken)
    {
        var entity = await _repository.GetByIdAsync(request.ServiceRequestId, cancellationToken)
            ?? throw new InvalidOperationException($"ServiceRequest {request.ServiceRequestId} not found.");

        var currentUserId = _info.UserInfoAccessor.UserInfo.UserId;
        var req = request.Request;
        var attachment = ServiceRequestAttachmentEntity.Create(
            entity.Id, req.FileId, req.AttachmentType, req.Title, req.Description,
            currentUserId, ServiceRequestActorType.Owner);
        entity.AddAttachment(attachment);
        _repository.Update(entity);

        return new AddServiceRequestAttachmentResponse(attachment.ToDto());
    }
}
