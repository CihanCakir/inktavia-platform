using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Bff.AdminPanel.Application.Contracts.Content;
using Aizen.Core.CQRS.Handler;

namespace Aizen.Bff.AdminPanel.Application.Content.Command;

[DocumentationInfo("Upsert content translation command handler", "İçerik öğesine tek dilli çeviri ekler/günceller.")]
public sealed class UpsertContentTranslationBffCommandHandler
    : AizenCommandHandler<UpsertContentTranslationBffCommand, ContentItemBffDto>
{
    private readonly IContentRemoteCall _content;

    public UpsertContentTranslationBffCommandHandler(IContentRemoteCall content) => _content = content;

    public override async Task<ContentItemBffDto?> Handle(
        UpsertContentTranslationBffCommand request, CancellationToken cancellationToken)
    {
        var result = await _content.UpsertTranslation(request.Id, request.Body);
        return ContentBffMapper.ToBff(result?.Body);
    }
}
