using Aizen.Bff.Marine.Web.Application.Common.Services;
using Aizen.Core.Messagebus.Abstraction.Consumers;
using Aizen.Core.Messagebus.Abstraction.Messages;
using Aizen.Modules.Content.Abstraction.Message;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Aizen.Bff.Marine.Web.Consumers;

/// <summary>
/// Bridges the Content module's <see cref="ContentPublishedMessage"/> broadcast to a cache-revalidation call on the
/// Next.js server, so the website can revalidate the item's tag instead of polling. The Content module is NOT
/// modified — this BFF only consumes the published event contract. Best-effort: a webhook failure never fails the
/// consume (the content is already published). <c>lang</c> is null — the event is language-agnostic.
/// </summary>
public sealed class ContentPublishedRevalidationConsumer : AizenBaseMessageConsumer<ContentPublishedMessage>
{
    private readonly IWebRevalidationNotifier _notifier;
    private readonly ILogger<ContentPublishedRevalidationConsumer> _logger;

    public ContentPublishedRevalidationConsumer(IServiceProvider sp) : base(sp)
    {
        _notifier = sp.GetRequiredService<IWebRevalidationNotifier>();
        _logger = sp.GetRequiredService<ILogger<ContentPublishedRevalidationConsumer>>();
    }

    public override Task<bool> ExecutePrepareMessage(ContentPublishedMessage message, CancellationToken ct)
        => Task.FromResult(true);

    public override async Task ExecuteCommitMessage(ContentPublishedMessage message, CancellationToken ct)
    {
        await _notifier.NotifyAsync(
            new WebRevalidationPayload("content", message.ContentId, message.Slug, Lang: null, "published"), ct);
    }

    public override Task ExecuteRollbackMessage(ContentPublishedMessage message, AizenMessageError ex, CancellationToken ct)
    {
        _logger.LogWarning("Rollback: ContentPublishedRevalidationConsumer for {ContentId} ({Slug}): {Error}",
            message.ContentId, message.Slug, ex.Message);
        return Task.CompletedTask;
    }
}
