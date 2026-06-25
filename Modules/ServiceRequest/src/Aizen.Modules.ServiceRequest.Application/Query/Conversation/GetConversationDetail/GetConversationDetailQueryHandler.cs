using Aizen.Core.CQRS.Handler;
using Aizen.Modules.ServiceRequest.Abstraction.Model;
using Aizen.Modules.ServiceRequest.Abstraction.Response.Conversation;
using Aizen.Modules.ServiceRequest.Domain.Interface.Repository;

namespace Aizen.Modules.ServiceRequest.Application.Query.Conversation;

[DocumentationInfo("Get conversation detail query handler", "Returns conversation with all messages and attachments.")]
public sealed class GetConversationDetailQueryHandler : AizenQueryHandler<GetConversationDetailQuery, GetConversationDetailResponse>
{
    private readonly IServiceRequestConversationRepository _repository;

    public GetConversationDetailQueryHandler(IServiceRequestConversationRepository repository) => _repository = repository;

    public override async Task<GetConversationDetailResponse> Handle(GetConversationDetailQuery request, CancellationToken cancellationToken)
    {
        var conversation = await _repository.GetByIdWithMessagesAsync(request.ConversationId, cancellationToken)
            ?? throw new InvalidOperationException($"Conversation {request.ConversationId} not found.");

        var messages = conversation.Messages
            .OrderBy(m => m.SentAt)
            .Select(m => new ChatMessageDto
            {
                Id = m.Id.ToString(),
                SenderId = m.SenderId,
                SenderName = m.SenderName,
                SenderRole = m.SenderRole,
                Content = m.Content,
                Timestamp = m.SentAt,
                Attachments = m.Attachments.Select(a => new MessageAttachmentItemDto
                {
                    Url = string.Empty,
                    Name = a.Name,
                    Type = a.Type
                }).ToList()
            }).ToList();

        var detail = new ConversationDetailDto
        {
            Id = conversation.Id.ToString(),
            Title = conversation.Title,
            ServiceRequestId = conversation.ServiceRequestId.ToString(),
            Messages = messages
        };

        return new GetConversationDetailResponse(detail);
    }
}
