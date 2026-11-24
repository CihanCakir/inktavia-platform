using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Notification.Abstraction.Response;
using Aizen.Modules.Notification.Domain.Interface.Repository;
using Microsoft.Extensions.Logging;

namespace Aizen.Modules.Notification.Application.Command.UnsubscribeNewsletter;

/// <summary>
/// P1 — one-click unsubscribe. Idempotent by design: the unsubscribe token is stable (never cleared), so a second call
/// finds the same (already-Unsubscribed) row and the domain <c>Unsubscribe()</c> is a no-op. An unknown token still
/// returns success (a caller learns nothing).
/// </summary>
public sealed class UnsubscribeNewsletterCommandHandler
    : AizenCommandHandler<UnsubscribeNewsletterCommand, NewsletterUnsubscribeResponse>
{
    private readonly INewsletterSubscriberRepository _repository;
    private readonly ILogger<UnsubscribeNewsletterCommandHandler> _logger;

    public UnsubscribeNewsletterCommandHandler(
        INewsletterSubscriberRepository repository,
        ILogger<UnsubscribeNewsletterCommandHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public override async Task<NewsletterUnsubscribeResponse?> Handle(
        UnsubscribeNewsletterCommand request, CancellationToken cancellationToken)
    {
        var token = request.Token?.Trim();
        if (string.IsNullOrEmpty(token))
            return Done();

        var entity = await _repository.GetByUnsubscribeTokenAsync(token, cancellationToken);
        if (entity is null)
            return Done();   // unknown token → still success, leaks nothing

        entity.Unsubscribe();   // idempotent
        await _repository.UpdateAsync(entity, cancellationToken);

        _logger.LogInformation("Newsletter subscriber {Id} unsubscribed.", entity.Id);
        return Done();
    }

    private static NewsletterUnsubscribeResponse Done() => new() { Unsubscribed = true };
}
