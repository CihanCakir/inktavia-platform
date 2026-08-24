using Aizen.Core.CQRS.Handler;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.Notification.Abstraction.Dto;
using Aizen.Modules.Notification.Application.Mapping;
using Aizen.Modules.Notification.Domain.Interface.Repository;

namespace Aizen.Modules.Notification.Application.Command.PublishTemplateContent;

/// <summary>
/// Yayınlama: (channel, locale) için Draft → Published; ÖNCEKİ Published → Archived. Command handler olduğu için
/// CQRS dekoratörü tüm handler'ı TEK transaction'a sarar → iki yazım (arşivle + yayınla) atomik commit edilir.
/// </summary>
public sealed class PublishTemplateContentCommandHandler
    : AizenCommandHandler<PublishTemplateContentCommand, NotificationTemplateContentDto>
{
    private readonly INotificationTemplateRepository        _templateRepository;
    private readonly INotificationTemplateContentRepository _contentRepository;

    public PublishTemplateContentCommandHandler(
        INotificationTemplateRepository templateRepository,
        INotificationTemplateContentRepository contentRepository)
    {
        _templateRepository = templateRepository;
        _contentRepository  = contentRepository;
    }

    public override async Task<NotificationTemplateContentDto?> Handle(
        PublishTemplateContentCommand request, CancellationToken cancellationToken)
    {
        var template = await _templateRepository.GetByCodeAsync(request.Code, cancellationToken)
                       ?? throw new AizenBusinessException($"Template '{request.Code}' not found.");

        var locale = request.Locale.ToLowerInvariant();

        var draft = await _contentRepository.GetCurrentDraftAsync(
            template.Id, request.Channel, locale, cancellationToken)
            ?? throw new AizenBusinessException(
                $"No draft to publish for '{request.Code}' {request.Channel}/{locale}.");

        // Önceki Published'ı arşivle (varsa) → aynı transaction içinde.
        var currentPublished = await _contentRepository.GetCurrentPublishedAsync(
            template.Id, request.Channel, locale, cancellationToken);
        if (currentPublished is not null)
        {
            currentPublished.Archive();
            await _contentRepository.UpdateAsync(currentPublished, cancellationToken);
        }

        draft.Publish();
        await _contentRepository.UpdateAsync(draft, cancellationToken);

        return draft.ToDto();
    }
}
