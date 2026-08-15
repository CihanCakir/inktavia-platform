using Aizen.Bff.Marine.Web.Application.Common.Services;
using Aizen.Core.Messagebus.Abstraction.Consumers;
using Aizen.Core.Messagebus.Abstraction.Messages;
using Aizen.Modules.Content.Abstraction.Message;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Aizen.Bff.Marine.Web.Consumers;

/// <summary>
/// Mirror of <see cref="ContentPublishedRevalidationConsumer"/> for <see cref="ContentUnpublishedMessage"/>
/// (unpublish / archive), so the website drops the item from its cached surfaces. Best-effort; <c>lang</c> is null.
/// </summary>
public sealed class ContentUnpublishedRevalidationConsumer : AizenBaseMessageConsumer<ContentUnpublishedMessage>
{
    private readonly IWebRevalidationNotifier _notifier;
    private readonly ILogger<ContentUnpublishedRevalidationConsumer> _logger;

    public ContentUnpublishedRevalidationConsumer(IServiceProvider sp) : base(sp)
    {
        _notifier = sp.GetRequiredService<IWebRevalidationNotifier>();
        _logger = sp.GetRequiredService<ILogger<ContentUnpublishedRevalidationConsumer>>();
    }

    public override Task<bool> ExecutePrepareMessage(ContentUnpublishedMessage message, CancellationToken ct)
        => Task.FromResult(true);

    public override async Task ExecuteCommitMessage(ContentUnpublishedMessage message, CancellationToken ct)
    {
        await _notifier.NotifyAsync(
            new WebRevalidationPayload("content", message.ContentId, message.Slug, Lang: null, "unpublished"), ct);
    }

    public override Task ExecuteRollbackMessage(ContentUnpublishedMessage message, AizenMessageError ex, CancellationToken ct)
    {
        _logger.LogWarning("Rollback: ContentUnpublishedRevalidationConsumer for {ContentId} ({Slug}): {Error}",
            message.ContentId, message.Slug, ex.Message);
        return Task.CompletedTask;
    }
}
