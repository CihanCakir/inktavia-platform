using Aizen.Core.CQRS.Handler;
using Aizen.Core.InfoAccessor.Abstraction;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Core.Messagebus.Abstraction.Senders;
using Aizen.Modules.Content.Abstraction.Dto;
using Aizen.Modules.Content.Abstraction.Enum;
using Aizen.Modules.Content.Abstraction.Message;
using Aizen.Modules.Content.Application.Services;
using Aizen.Modules.Content.Domain.Interface.Repository;
using Microsoft.Extensions.Logging;

namespace Aizen.Modules.Content.Application.Commands.UnpublishContentItem;

public sealed class UnpublishContentItemCommandHandler
    : AizenCommandHandler<UnpublishContentItemCommand, ContentItemDto>
{
    public override bool IsTransactional => false;

    private readonly IContentItemRepository _items;
    private readonly IAizenInfoAccessor _info;
    private readonly IAizenMessagePublisher _publisher;
    private readonly IContentCacheInvalidator _cache;
    private readonly ILogger<UnpublishContentItemCommandHandler> _logger;

    public UnpublishContentItemCommandHandler(
        IContentItemRepository items,
        IAizenInfoAccessor info,
        IAizenMessagePublisher publisher,
        IContentCacheInvalidator cache,
        ILogger<UnpublishContentItemCommandHandler> logger)
    {
        _items = items;
        _info = info;
        _publisher = publisher;
        _cache = cache;
        _logger = logger;
    }

    public override async Task<ContentItemDto?> Handle(
        UnpublishContentItemCommand request, CancellationToken cancellationToken)
    {
        var item = await _items.GetByIdAsync(request.ContentId, cancellationToken)
            ?? throw new AizenBusinessException($"Content '{request.ContentId}' was not found.");

        ContentAuthorization.EnsureElevated(_info, "Unpublish content");
        ContentStatusTransition.EnsureCanUnpublish(item.Status);

        var now = DateTimeOffset.UtcNow;
        item.Status = ContentStatus.Draft;
        item.UpdatedAt = now;

        await _items.ReplaceAsync(item, cancellationToken);

        var surfaces = item.Placements.Select(p => p.Surface).Distinct().ToList();
        try
        {
            await _publisher.PublishAsync(new ContentUnpublishedMessage
            {
                ContentId = item.Id,
                Slug = item.Slug,
                Type = item.Type.ToString(),
                Surfaces = surfaces.Select(s => s.ToString()).ToArray(),
                AudienceType = item.Audience.Type.ToString(),
                UnpublishedAt = now,
            }, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Failed to publish ContentUnpublishedMessage for ContentId={Id}.", item.Id);
        }

        await _cache.BumpAsync(surfaces, cancellationToken);

        return ContentMapper.ToDto(item);
    }
}
