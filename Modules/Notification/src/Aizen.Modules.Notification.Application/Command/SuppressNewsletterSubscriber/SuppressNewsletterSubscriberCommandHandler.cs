using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Notification.Abstraction.Dto;
using Aizen.Modules.Notification.Application.Mapping;
using Aizen.Modules.Notification.Domain.Interface.Repository;
using Microsoft.Extensions.Logging;

namespace Aizen.Modules.Notification.Application.Command.SuppressNewsletterSubscriber;

public sealed class SuppressNewsletterSubscriberCommandHandler
    : AizenCommandHandler<SuppressNewsletterSubscriberCommand, NewsletterSubscriberListItemDto>
{
    private readonly INewsletterSubscriberRepository _repository;
    private readonly ILogger<SuppressNewsletterSubscriberCommandHandler> _logger;

    public SuppressNewsletterSubscriberCommandHandler(
        INewsletterSubscriberRepository repository,
        ILogger<SuppressNewsletterSubscriberCommandHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public override async Task<NewsletterSubscriberListItemDto?> Handle(
        SuppressNewsletterSubscriberCommand request, CancellationToken cancellationToken)
    {
        var entity = await _repository.GetByIdAsync(request.Id, cancellationToken);
        if (entity is null)
            return null;

        entity.Suppress();
        await _repository.UpdateAsync(entity, cancellationToken);

        _logger.LogWarning("Newsletter subscriber {Id} manually SUPPRESSED by admin.", entity.Id);
        return entity.ToListItemDto();
    }
}
