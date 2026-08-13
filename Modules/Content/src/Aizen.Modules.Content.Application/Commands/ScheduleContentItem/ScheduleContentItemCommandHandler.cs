using Aizen.Core.CQRS.Handler;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.Content.Abstraction.Dto;
using Aizen.Modules.Content.Abstraction.Enum;
using Aizen.Modules.Content.Application.Services;
using Aizen.Modules.Content.Domain.Interface.Repository;

namespace Aizen.Modules.Content.Application.Commands.ScheduleContentItem;

public sealed class ScheduleContentItemCommandHandler
    : AizenCommandHandler<ScheduleContentItemCommand, ContentItemDto>
{
    public override bool IsTransactional => false;

    private readonly IContentItemRepository _items;

    public ScheduleContentItemCommandHandler(IContentItemRepository items) => _items = items;

    public override async Task<ContentItemDto?> Handle(
        ScheduleContentItemCommand request, CancellationToken cancellationToken)
    {
        var item = await _items.GetByIdAsync(request.ContentId, cancellationToken)
            ?? throw new AizenBusinessException($"Content '{request.ContentId}' was not found.");

        ContentStatusTransition.EnsureCanSchedule(item.Status);

        item.PublishAt = request.PublishAt;
        item.ExpireAt = request.ExpireAt;
        item.DateKey = request.PublishAt.UtcDateTime.ToString("yyyy-MM-dd");
        item.Status = ContentStatus.Scheduled;
        item.UpdatedAt = DateTimeOffset.UtcNow;

        await _items.ReplaceAsync(item, cancellationToken);
        return ContentMapper.ToDto(item);
    }
}
