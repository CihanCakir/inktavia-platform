using Aizen.Core.CQRS.Handler;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.Notification.Abstraction.Dto;
using Aizen.Modules.Notification.Abstraction.Enum;
using Aizen.Modules.Notification.Application.Mapping;
using Aizen.Modules.Notification.Domain.Interface.Repository;

namespace Aizen.Modules.Notification.Application.Query.GetTemplateContentForEdit;

/// <summary>
/// Editörün düzenlemeden ÖNCE mevcut içeriği göstermesi için tek hücrenin (channel×locale) düzenlenebilir içeriği:
/// önce mevcut Draft (varsa) — çünkü kaydetme akışı Draft'ı günceller — yoksa mevcut Published, o da yoksa boş sonuç.
/// Yeni sorgu yazmaz: repo'nun mevcut GetCurrentDraftAsync/GetCurrentPublishedAsync metotlarını kullanır.
/// </summary>
public sealed class GetTemplateContentForEditQueryHandler
    : AizenQueryHandler<GetTemplateContentForEditQuery, NotificationTemplateContentEditResult>
{
    private readonly INotificationTemplateRepository        _templateRepository;
    private readonly INotificationTemplateContentRepository _contentRepository;

    public GetTemplateContentForEditQueryHandler(
        INotificationTemplateRepository templateRepository,
        INotificationTemplateContentRepository contentRepository)
    {
        _templateRepository = templateRepository;
        _contentRepository  = contentRepository;
    }

    public override async Task<NotificationTemplateContentEditResult> Handle(
        GetTemplateContentForEditQuery request, CancellationToken cancellationToken)
    {
        var template = await _templateRepository.GetByCodeAsync(request.Code, cancellationToken)
                       ?? throw new AizenBusinessException($"Template '{request.Code}' not found.");

        // Draft öncelikli: kaydetme akışı Draft üzerinden yürür, dolayısıyla editör en güncel düzenlenebilir hâli görmeli.
        var draft = await _contentRepository.GetCurrentDraftAsync(
            template.Id, request.Channel, request.Locale, cancellationToken);
        if (draft is not null)
        {
            return new NotificationTemplateContentEditResult
            {
                Found         = true,
                EditingSource = TemplateEditingSource.Draft,
                Content       = draft.ToDto(),
            };
        }

        // Draft yoksa mevcut Published gösterilir; düzenleme bunun üstünden yeni bir Draft üretecek.
        var published = await _contentRepository.GetCurrentPublishedAsync(
            template.Id, request.Channel, request.Locale, cancellationToken);
        if (published is not null)
        {
            return new NotificationTemplateContentEditResult
            {
                Found         = true,
                EditingSource = TemplateEditingSource.Published,
                Content       = published.ToDto(),
            };
        }

        // Bu hücre için hiç içerik yok — editör boş başlar.
        return new NotificationTemplateContentEditResult
        {
            Found         = false,
            EditingSource = TemplateEditingSource.None,
            Content       = null,
        };
    }
}
