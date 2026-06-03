using Aizen.Core.CQRS.Handler;
using Aizen.Modules.ServiceRequest.Abstraction.Model;
using Aizen.Modules.ServiceRequest.Domain.Interface.Repository;

namespace Aizen.Modules.ServiceRequest.Application.Command.Message;

[DocumentationInfo("Mark messages read command handler", "Marks specified service request messages as read.")]
public sealed class MarkServiceRequestMessagesReadCommandHandler : AizenCommandHandler<MarkServiceRequestMessagesReadCommand, bool>
{
    private readonly IServiceRequestMessageRepository _messageRepository;

    public MarkServiceRequestMessagesReadCommandHandler(IServiceRequestMessageRepository messageRepository)
    {
        _messageRepository = messageRepository;
    }

    public override async Task<bool> Handle(MarkServiceRequestMessagesReadCommand request, CancellationToken cancellationToken)
    {
        var messages = await _messageRepository
            .GetByServiceRequestIdAsync(request.ServiceRequestId, 0, int.MaxValue, cancellationToken);

        var toMark = messages
            .Where(m => request.Request.MessageIds.Contains(m.Id) && !m.IsRead)
            .ToList();

        foreach (var msg in toMark)
        {
            msg.MarkAsRead();
            _messageRepository.Update(msg);
        }

        return true;
    }
}
