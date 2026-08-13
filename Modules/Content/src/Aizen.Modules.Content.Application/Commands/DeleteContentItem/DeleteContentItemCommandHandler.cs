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

namespace Aizen.Modules.Content.Application.Commands.DeleteContentItem;

public sealed class DeleteContentItemCommandHandler
    : AizenCommandHandler<DeleteContentItemCommand, ContentItemDto>
{
    public override bool IsTransactional => false;

    private readonly IContentItemRepository _items;
    private readonly IAizenInfoAccessor _info;
    private readonly IAizenMessagePublisher _publisher;
    private readonly IContentCacheInvalidator _cache;
    private readonly ILogger<DeleteContentItemCommandHandler> _logger;

    public DeleteContentItemCommandHandler(
        IContentItemRepository items,
        IAizenInfoAccessor info,
        IAizenMessagePublisher publisher,
        IContentCacheInvalidator cache,
        ILogger<DeleteContentItemCommandHandler> logger)
    {
        _items = items;
        _info = info;
        _publisher = publisher;
        _cache = cache;
        _logger = logger;
    }

    public override async Task<ContentItemDto?> Handle(
        DeleteContentItemCommand request, CancellationToken cancellationToken)
    {
        var item = await _items.GetByIdAsync(request.ContentId, cancellationToken)
            ?? throw new AizenBusinessException($"Content '{request.ContentId}' was not found.");

        ContentAuthorization.EnsureElevated(_info, "Delete content");

        var wasPublished = item.Status == ContentStatus.Published;
        var snapshot = ContentMapper.ToDto(item);

        await _items.SoftDeleteAsync(item.Id, cancellationToken);

        // Deleting a live (published) item removes it from public surfaces — treat as an unpublish edge.
        if (wasPublished)
        {
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
                    UnpublishedAt = DateTimeOffset.UtcNow,
                }, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "Failed to publish ContentUnpublishedMessage on delete for ContentId={Id}.", item.Id);
            }

            await _cache.BumpAsync(surfaces, cancellationToken);
        }

        return snapshot;
    }
}
