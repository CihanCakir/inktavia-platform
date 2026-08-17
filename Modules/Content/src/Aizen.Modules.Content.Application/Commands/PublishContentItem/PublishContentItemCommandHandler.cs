using Aizen.Core.CQRS.Handler;
using Aizen.Core.InfoAccessor.Abstraction;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Core.Messagebus.Abstraction.Senders;
using Aizen.Modules.Content.Abstraction.Dto;
using Aizen.Modules.Content.Abstraction.Enum;
using Aizen.Modules.Content.Abstraction.Message;
using Aizen.Modules.Content.Application.Services;
using Aizen.Modules.Content.Domain.Interface.Repository;
using Aizen.Modules.Content.Domain.MongoDocuments;
using Microsoft.Extensions.Logging;

namespace Aizen.Modules.Content.Application.Commands.PublishContentItem;

public sealed class PublishContentItemCommandHandler
    : AizenCommandHandler<PublishContentItemCommand, ContentItemDto>
{
    public override bool IsTransactional => false;

    private readonly IContentItemRepository _items;
    private readonly IAizenInfoAccessor _info;
    private readonly IAizenMessagePublisher _publisher;
    private readonly IContentCacheInvalidator _cache;
    private readonly ILogger<PublishContentItemCommandHandler> _logger;

    public PublishContentItemCommandHandler(
        IContentItemRepository items,
        IAizenInfoAccessor info,
        IAizenMessagePublisher publisher,
        IContentCacheInvalidator cache,
        ILogger<PublishContentItemCommandHandler> logger)
    {
        _items = items;
        _info = info;
        _publisher = publisher;
        _cache = cache;
        _logger = logger;
    }

    public override async Task<ContentItemDto?> Handle(
        PublishContentItemCommand request, CancellationToken cancellationToken)
    {
        var item = await _items.GetByIdAsync(request.ContentId, cancellationToken)
            ?? throw new AizenBusinessException($"Content '{request.ContentId}' was not found.");

        ContentAuthorization.EnsureElevated(_info, "Publish content");
        ContentStatusTransition.EnsureCanPublish(item.Status);
        EnsurePublishable(item);

        var now = DateTimeOffset.UtcNow;
        item.Status = ContentStatus.Published;
        item.PublishAt ??= now;
        item.PublishedAt = now;
        item.DateKey = (item.PublishAt ?? now).UtcDateTime.ToString("yyyy-MM-dd");
        item.UpdatedAt = now;

        await _items.ReplaceAsync(item, cancellationToken);

        var surfaces = item.Placements.Select(p => p.Surface).Distinct().ToList();

        // Best-effort integration event (B2) — a bus hiccup must never fail the write.
        try
        {
            await _publisher.PublishAsync(new ContentPublishedMessage
            {
                ContentId = item.Id,
                Slug = item.Slug,
                Type = item.Type.ToString(),
                Surfaces = surfaces.Select(s => s.ToString()).ToArray(),
                AudienceType = item.Audience.Type.ToString(),
                PublishedAt = now,
            }, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Failed to publish ContentPublishedMessage for ContentId={Id}; Notification fan-out skipped.", item.Id);
        }

        await _cache.BumpAsync(surfaces, cancellationToken);

        return ContentMapper.ToDto(item);
    }

    private static void EnsurePublishable(ContentItemDocument item)
    {
        var hasDefaultTranslation = item.Translations.Any(t =>
            string.Equals(t.Lang, item.DefaultLanguage, StringComparison.OrdinalIgnoreCase)
            && !string.IsNullOrWhiteSpace(t.Title));
        if (!hasDefaultTranslation)
            throw new AizenBusinessException(
                $"Cannot publish: a translation for the default language '{item.DefaultLanguage}' with a title is required.");

        if (item.Placements.Count == 0)
            throw new AizenBusinessException("Cannot publish: at least one placement is required.");
    }
}
