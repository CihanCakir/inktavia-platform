using Aizen.Core.Cache.Abstraction;
using Aizen.Core.Cache.Abstraction.Common;
using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Notification.Abstraction.Enum;
using Aizen.Modules.Notification.Abstraction.Response;
using Aizen.Modules.Notification.Domain.Entities;
using Aizen.Modules.Notification.Domain.Interface.Repository;
using Aizen.Modules.Notification.Domain.Interface.Service;
using Microsoft.Extensions.Logging;

namespace Aizen.Modules.Notification.Application.Command.SendNotification;

public sealed class SendNotificationCommandHandler
    : AizenCommandHandler<SendNotificationCommand, SendNotificationResponse>
{
    private readonly INotificationRepository         _notificationRepository;
    private readonly INotificationTemplateRepository _templateRepository;
    private readonly INotificationDispatcher         _dispatcher;
    private readonly ITemplateInterpolator           _interpolator;
    private readonly IAizenDistributedCache           _cache;
    private readonly ILogger<SendNotificationCommandHandler> _logger;

    public SendNotificationCommandHandler(
        INotificationRepository notificationRepository,
        INotificationTemplateRepository templateRepository,
        INotificationDispatcher dispatcher,
        ITemplateInterpolator interpolator,
        IAizenDistributedCache cache,
        ILogger<SendNotificationCommandHandler> logger)
    {
        _notificationRepository = notificationRepository;
        _templateRepository     = templateRepository;
        _dispatcher             = dispatcher;
        _interpolator           = interpolator;
        _cache                  = cache;
        _logger                 = logger;
    }

    public override async Task<SendNotificationResponse?> Handle(
        SendNotificationCommand request, CancellationToken cancellationToken)
    {
        var template = await _templateRepository
            .GetActiveByTypeAndChannelAsync(request.Type, request.Channel, cancellationToken);

        if (template is null)
        {
            _logger.LogWarning(
                "No active notification template found for Type={Type} Channel={Channel}. Skipping.",
                request.Type, request.Channel);
            return new SendNotificationResponse { NotificationId = 0, Dispatched = false };
        }

        var title = _interpolator.Interpolate(template.TitleTemplate, request.Variables);
        var body  = _interpolator.Interpolate(template.BodyTemplate,  request.Variables);

        var entity = NotificationEntity.Create(
            request.RecipientUserId, request.Type, request.Channel,
            template.TemplateCode, title, body, request.MetadataJson);

        await _notificationRepository.AddAsync(entity, cancellationToken);

        try
        {
            await _dispatcher.DispatchAsync(entity, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Dispatch failed for NotificationId={Id} Channel={Channel}",
                entity.Id, request.Channel);
            entity.MarkAsFailed();
            await _notificationRepository.UpdateAsync(entity, cancellationToken);
        }

        // Redact sensitive content (e.g. OTP) from the persisted body after dispatch
        if (request.Type == NotificationType.PasswordRecoveryOtp
            && request.Variables.TryGetValue("otp", out var otp)
            && !string.IsNullOrEmpty(otp))
        {
            var redactedBody = body.Replace(otp, new string('\u2022', otp.Length));
            entity.RedactBody(redactedBody);
            await _notificationRepository.UpdateAsync(entity, cancellationToken);
        }

        // Bump recipient's cache generation so their notification list refreshes
        await BumpGenerationAsync(request.RecipientUserId, cancellationToken);

        return new SendNotificationResponse
        {
            NotificationId = entity.Id,
            Dispatched     = entity.Status == NotificationStatus.Sent,
        };
    }

    private async Task BumpGenerationAsync(long rid, CancellationToken ct)
    {
        try
        {
            await _cache.SetAsync(
                DateTimeOffset.UtcNow.Ticks,
                $"notif:gen:{rid}",
                new AizenCacheOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10) },
                ct);
        }
        catch { /* cache failure must not break the write path */ }
    }
}
