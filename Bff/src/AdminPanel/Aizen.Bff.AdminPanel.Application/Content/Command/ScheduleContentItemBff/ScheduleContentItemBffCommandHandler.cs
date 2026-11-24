using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Bff.AdminPanel.Application.Contracts.Content;
using Aizen.Core.CQRS.Handler;

namespace Aizen.Bff.AdminPanel.Application.Content.Command;

[DocumentationInfo("Schedule content item command handler", "İçerik öğesini yayına programlar (Draft → Scheduled).")]
public sealed class ScheduleContentItemBffCommandHandler
    : AizenCommandHandler<ScheduleContentItemBffCommand, ContentItemBffDto>
{
    private readonly IContentRemoteCall _content;

    public ScheduleContentItemBffCommandHandler(IContentRemoteCall content) => _content = content;

    public override async Task<ContentItemBffDto?> Handle(
        ScheduleContentItemBffCommand request, CancellationToken cancellationToken)
    {
        var result = await _content.Schedule(request.Id, request.Body);
        return ContentBffMapper.ToBff(result?.Body);
    }
}
