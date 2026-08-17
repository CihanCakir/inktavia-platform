using Aizen.Core.Cache.Abstraction;
using Aizen.Core.Cache.Abstraction.Common;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.Messagebus.Abstraction.Senders;
using Aizen.Modules.Notification.Abstraction;
using Aizen.Modules.Notification.Abstraction.Enum;
using Aizen.Modules.Notification.Abstraction.Message;
using Aizen.Modules.Notification.Abstraction.Response;
using Aizen.Modules.Notification.Domain.Entities;
using Aizen.Modules.Notification.Domain.Interface.Repository;
using Aizen.Modules.Notification.Domain.Interface.Service;
using Microsoft.Extensions.Logging;

namespace Aizen.Modules.Notification.Application.Command.SendNotification;

public sealed class SendNotificationCommandHandler
    : AizenCommandHandler<SendNotificationCommand, SendNotificationResponse>
{
    private readonly INotificationRepository           _notificationRepository;
    private readonly INotificationTemplateRepository   _templateRepository;
    private readonly INotificationPreferenceRepository _preferenceRepository;
    private readonly INotificationDispatcher           _dispatcher;
    private readonly ITemplateInterpolator             _interpolator;
    private readonly IAizenDistributedCache            _cache;
    private readonly IAizenMessagePublisher            _publisher;
    private readonly ILogger<SendNotificationCommandHandler> _logger;

    public SendNotificationCommandHandler(
        INotificationRepository notificationRepository,
        INotificationTemplateRepository templateRepository,
        INotificationPreferenceRepository preferenceRepository,
        INotificationDispatcher dispatcher,
        ITemplateInterpolator interpolator,
        IAizenDistributedCache cache,
        IAizenMessagePublisher publisher,
        ILogger<SendNotificationCommandHandler> logger)
    {
        _notificationRepository = notificationRepository;
        _templateRepository     = templateRepository;
        _preferenceRepository   = preferenceRepository;
        _dispatcher             = dispatcher;
        _interpolator           = interpolator;
        _cache                  = cache;
        _publisher              = publisher;
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

        // N-B preference gate — Email only. InApp is the always-on baseline; Push is gated in
        // NotificationSentPushConsumer (so its in-app row still persists). Security/Account emails always deliver.
        if (request.Channel == NotificationChannel.Email)
        {
            var emailCategory = NotificationCategoryMap.Resolve(request.Type);
            var prefs = await _preferenceRepository.GetByUserAsync(request.RecipientUserId, cancellationToken);
            var storedEmail = prefs
                .Where(p => p.Category == emailCategory && p.Channel == NotificationChannel.Email)
                .Select(p => (bool?)p.Enabled)
                .FirstOrDefault();
            if (!NotificationPreferencePolicy.Resolve(emailCategory, NotificationChannel.Email, storedEmail))
            {
                _logger.LogInformation(
                    "Email muted for UserId={Uid} category={Cat}; email notification skipped for Type={Type}.",
                    request.RecipientUserId, emailCategory, request.Type);
                return new SendNotificationResponse { NotificationId = 0, Dispatched = false };
            }
        }

        var title = _interpolator.Interpolate(template.TitleTemplate, request.Variables);
        var body  = _interpolator.Interpolate(template.BodyTemplate,  request.Variables);

        var entity = NotificationEntity.Create(
            request.RecipientUserId, request.Type, request.Channel,
            template.TemplateCode, title, body, request.MetadataJson,
            request.ReferenceType, request.ReferenceId);

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

        // Realtime edge (ADR: BFF-hosted hubs, modules publish-only). Publish a thin "notification created" event so a
        // BFF-hosted notification hub can push a live badge refresh to THIS recipient. In-app only — Email/SMS do not
        // drive the in-app badge. The frame carries no body; the BFF maps it to a per-recipient refetch hint. Best-effort:
        // a bus hiccup must never fail the notification write (the REST inbox/badge poll still reflects it).
        if (request.Channel == NotificationChannel.InApp)
        {
            try
            {
                await _publisher.PublishAsync(new NotificationSentMessage
                {
                    NotificationId  = entity.Id,
                    RecipientUserId = request.RecipientUserId,
                    Type            = request.Type,
                    Channel         = request.Channel,
                    Status          = entity.Status,
                    Title           = title,
                    SentAt          = DateTimeOffset.UtcNow,
                    ReferenceType   = request.ReferenceType,
                    ReferenceId     = request.ReferenceId,
                }, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "Failed to publish NotificationSentMessage for NotificationId={Id}; live badge refresh skipped.",
                    entity.Id);
            }
        }

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
