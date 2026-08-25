using Aizen.Core.CQRS.Handler;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.Notification.Abstraction.Dto;
using Aizen.Modules.Notification.Abstraction.Enum;
using Aizen.Modules.Notification.Application.Mapping;
using Aizen.Modules.Notification.Application.Services;
using Aizen.Modules.Notification.Domain.Entities;
using Aizen.Modules.Notification.Domain.Interface.Repository;

namespace Aizen.Modules.Notification.Application.Command.SaveTemplateContentDraft;

/// <summary>
/// Taslak kaydetme kuralı: (channel, locale) için MEVCUT Draft satırı varsa onu günceller; yoksa YENİ bir Draft sürüm
/// oluşturur (Version = mevcut max + 1). Published/Archived içerik ASLA mutasyona uğramaz.
/// </summary>
public sealed class SaveTemplateContentDraftCommandHandler
    : AizenCommandHandler<SaveTemplateContentDraftCommand, NotificationTemplateContentDto>
{
    private readonly INotificationTemplateRepository        _templateRepository;
    private readonly INotificationTemplateContentRepository _contentRepository;

    public SaveTemplateContentDraftCommandHandler(
        INotificationTemplateRepository templateRepository,
        INotificationTemplateContentRepository contentRepository)
    {
        _templateRepository = templateRepository;
        _contentRepository  = contentRepository;
    }

    public override async Task<NotificationTemplateContentDto?> Handle(
        SaveTemplateContentDraftCommand request, CancellationToken cancellationToken)
    {
        var template = await _templateRepository.GetByCodeAsync(request.Code, cancellationToken)
                       ?? throw new AizenBusinessException($"Template '{request.Code}' not found.");

        // SMS metni HTML içeremez ('<') — hem yeni draft hem mevcut draft güncellemesi bu tek noktada korunur.
        SmsContentValidator.EnsureNoHtml(request.SmsTextTemplate, request.Code);

        var locale = request.Locale.ToLowerInvariant();

        var existingDraft = await _contentRepository.GetCurrentDraftAsync(
            template.Id, request.Channel, locale, cancellationToken);

        if (existingDraft is not null)
        {
            // Mevcut taslağı güncelle (UpdateContent yalnız Draft'ta izinlidir — entity guard'lı).
            existingDraft.UpdateContent(
                request.TitleTemplate, request.BodyTemplate, request.SubjectTemplate, request.HtmlTemplate,
                request.TextTemplate, request.DeepLinkTemplate, request.SmsTextTemplate, request.LayoutCode);
            await _contentRepository.UpdateAsync(existingDraft, cancellationToken);
            return existingDraft.ToDto();
        }

        // Taslak yok → yeni Draft sürüm. Published'a dokunmadan max+1 versiyon.
        var maxVersion = await _contentRepository.GetMaxVersionAsync(
            template.Id, request.Channel, locale, cancellationToken);

        var draft = NotificationTemplateContentEntity.Create(
            templateId:      template.Id,
            channel:         request.Channel,
            locale:          locale,
            version:         maxVersion + 1,
            status:          TemplateContentStatus.Draft,
            titleTemplate:   request.TitleTemplate,
            bodyTemplate:    request.BodyTemplate,
            subjectTemplate: request.SubjectTemplate,
            htmlTemplate:    request.HtmlTemplate,
            textTemplate:    request.TextTemplate,
            deepLinkTemplate: request.DeepLinkTemplate,
            smsTextTemplate: request.SmsTextTemplate,
            layoutCode:      request.LayoutCode);

        await _contentRepository.AddAsync(draft, cancellationToken);
        return draft.ToDto();
    }
}
