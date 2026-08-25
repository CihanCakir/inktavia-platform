using Aizen.Core.CQRS.Handler;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.Notification.Abstraction.Dto;
using Aizen.Modules.Notification.Abstraction.Enum;
using Aizen.Modules.Notification.Application.Services;
using Aizen.Modules.Notification.Domain.Interface.Repository;
using Aizen.Modules.Notification.Domain.Interface.Service;

namespace Aizen.Modules.Notification.Application.Query.GetTemplateVariables;

public sealed class GetTemplateVariablesQueryHandler
    : AizenQueryHandler<GetTemplateVariablesQuery, NotificationTemplateVariablesDto>
{
    private readonly INotificationTemplateRepository        _templateRepository;
    private readonly INotificationTemplateContentRepository _contentRepository;
    private readonly ITemplateInterpolator                  _interpolator;

    public GetTemplateVariablesQueryHandler(
        INotificationTemplateRepository templateRepository,
        INotificationTemplateContentRepository contentRepository,
        ITemplateInterpolator interpolator)
    {
        _templateRepository = templateRepository;
        _contentRepository  = contentRepository;
        _interpolator       = interpolator;
    }

    public override async Task<NotificationTemplateVariablesDto> Handle(
        GetTemplateVariablesQuery request, CancellationToken cancellationToken)
    {
        var template = await _templateRepository.GetByCodeAsync(request.Code, cancellationToken)
                       ?? throw new AizenBusinessException($"Template '{request.Code}' not found.");

        // İzin verilen placeholder'lar template'in NotificationType'ından kataloğa göre türetilir.
        var placeholders = NotificationVariablesCatalog.GetAllowedPlaceholders(template.Type);

        // Katalog eksikse editör boş kalmasın — şablonun kendi içeriğinden çıkar. Migrate edilen şablonların bir
        // kısmının Type'ı hiçbir consumer tarafından yayınlanmaz (ör. ServiceRequestCreated → gerçek yayın yolu
        // ServiceRequestPublished'dır), dolayısıyla katalogda karşılığı yoktur. Bu durumda placeholder'ları
        // içeriğin ({{...}}) kendisinden — Draft/Published tüm alanlardan — türetip birleştiririz.
        if (placeholders.Count == 0)
            placeholders = await DerivePlaceholdersFromContentAsync(template.Id, cancellationToken);

        return new NotificationTemplateVariablesDto
        {
            TemplateCode = template.TemplateCode,
            Type         = template.Type,
            Placeholders = placeholders,
        };
    }

    private async Task<IReadOnlyList<string>> DerivePlaceholdersFromContentAsync(long templateId, CancellationToken ct)
    {
        var contents = await _contentRepository.GetAllByTemplateIdAsync(templateId, ct);

        var derived = new List<string>();
        // Yalnız düzenlenebilir içerik (Draft/Published); arşiv satırlarını yok say.
        foreach (var c in contents.Where(x => x.Status != TemplateContentStatus.Archived))
        {
            foreach (var field in new[]
                     {
                         c.SubjectTemplate, c.HtmlTemplate, c.TextTemplate, c.TitleTemplate,
                         c.BodyTemplate, c.DeepLinkTemplate, c.SmsTextTemplate,
                     })
            {
                foreach (var key in _interpolator.ExtractPlaceholders(field))
                    if (!derived.Contains(key))
                        derived.Add(key);
            }
        }

        return derived;
    }
}
