using Aizen.Core.CQRS.Handler;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Core.InfoAccessor.Abstraction;
using Aizen.Core.Messagebus.Abstraction.Senders;
using Aizen.Modules.CargoDry.Abstraction.Dto;
using Aizen.Modules.CargoDry.Abstraction.Enum;
using Aizen.Modules.CargoDry.Abstraction.Message;
using Aizen.Modules.CargoDry.Application.Commands.PrepareCargoDryKitRenewal;
using Aizen.Modules.CargoDry.Domain.Interface.Repository;
using Microsoft.Extensions.Logging;

namespace Aizen.Modules.CargoDry.Application.Commands.DispatchCargoDryRenewalNotification;

[DocumentationInfo("DispatchCargoDryRenewalNotificationCommandHandler",
    "Publishes CargoDryRenewalNotificationRequestedMessage to message bus. " +
    "Idempotent: if already in Queued/Dispatched status, returns without re-publishing. " +
    "Does NOT create PaymentTransaction (Hard rule #16). " +
    "CargoDry does NOT call SMS/Mail/Push providers directly (Hard rule #2). " +
    "Notification module consumer owns delivery. " +
    "Phase 11 (July 2026).")]
public sealed class DispatchCargoDryRenewalNotificationCommandHandler
    : AizenCommandHandler<DispatchCargoDryRenewalNotificationCommand, CargoDryRenewalPreparationDto>
{
    private readonly ICargoDryRenewalPreparationRepository _preparations;
    private readonly ICargoDryProductRepository            _products;
    private readonly IAizenMessagePublisher                _publisher;
    private readonly IAizenInfoAccessor                    _info;
    private readonly ILogger<DispatchCargoDryRenewalNotificationCommandHandler> _logger;

    public DispatchCargoDryRenewalNotificationCommandHandler(
        ICargoDryRenewalPreparationRepository                    preparations,
        ICargoDryProductRepository                               products,
        IAizenMessagePublisher                                   publisher,
        IAizenInfoAccessor                                       info,
        ILogger<DispatchCargoDryRenewalNotificationCommandHandler> logger)
    {
        _preparations = preparations;
        _products     = products;
        _publisher    = publisher;
        _info         = info;
        _logger       = logger;
    }

    public override async Task<CargoDryRenewalPreparationDto> Handle(
        DispatchCargoDryRenewalNotificationCommand request, CancellationToken ct)
    {
        var preparation = await _preparations.GetByIdAsync(request.RenewalPreparationId, ct)
            ?? throw new AizenBusinessException(
                $"Renewal preparation {request.RenewalPreparationId} not found.");

        // ── Idempotency ───────────────────────────────────────────────────────
        if (preparation.NotificationStatus is
            CargoDryRenewalNotificationStatus.Queued or
            CargoDryRenewalNotificationStatus.Dispatched)
        {
            _logger.LogInformation(
                "Renewal notification already dispatched. PreparationId={Id} Code={Code} Status={Status}",
                preparation.Id, preparation.RenewalCode, preparation.NotificationStatus);

            var productForIdem = await _products.GetByCodeAsync(preparation.ProductCode, ct);
            return PrepareCargoDryKitRenewalCommandHandler.MapToDto(preparation, productForIdem?.Name);
        }

        // ── State guards ──────────────────────────────────────────────────────
        if (preparation.IsTerminal)
            throw new AizenBusinessException(
                $"Renewal preparation {preparation.Id} ({preparation.RenewalCode}) is in terminal status " +
                $"{preparation.Status} — cannot dispatch notification.");

        if (!preparation.CanDispatchNotification)
            throw new AizenBusinessException(
                $"Renewal preparation {preparation.Id} ({preparation.RenewalCode}) cannot dispatch notification: " +
                "kit has no owner.");

        if (string.IsNullOrWhiteSpace(preparation.LastNotificationTemplateCode))
            throw new AizenBusinessException(
                $"Renewal preparation {preparation.Id} ({preparation.RenewalCode}) has no notification template configured. " +
                "Run PrepareCargoDryRenewalNotification first.");

        // ── Build correlation id and idempotency key ───────────────────────────
        var correlationId    = Guid.NewGuid().ToString("N");
        var dispatchedByUserId = _info.UserInfoAccessor.UserInfo.UserId;
        var idempotencyKey = $"RENEWAL-NOTIF-{preparation.RenewalCode}-{correlationId[..8]}";
        var channelsJson   = preparation.NotificationChannels ?? "[\"InApp\"]";
        var templateCode   = preparation.LastNotificationTemplateCode!;
        var languageCode   = preparation.LastNotificationLanguageCode ?? "tr";

        var daysUntilExpiry = preparation.CurrentExpiresAtUtc > DateTimeOffset.UtcNow
            ? (int)(preparation.CurrentExpiresAtUtc - DateTimeOffset.UtcNow).TotalDays
            : 0;

        // ── Publish to bus — Notification module consumer handles delivery ─────
        await _publisher.PublishAsync(new CargoDryRenewalNotificationRequestedMessage
        {
            CorrelationId        = correlationId,
            RenewalPreparationId = preparation.Id,
            RenewalCode          = preparation.RenewalCode,
            KitId                = preparation.KitId,
            KitCode              = preparation.KitCode,
            ProductCode          = preparation.ProductCode,
            OwnerUserId          = preparation.OwnerUserId,
            VesselId             = preparation.VesselId,
            ProviderProfileId    = preparation.ProviderProfileId,
            Channels             = channelsJson.Replace("[\"", "").Replace("\"]", "").Replace("\",\"", ","),
            LanguageCode         = languageCode,
            TemplateCode         = templateCode,
            RecipientEmail       = request.RecipientEmail,
            RecipientPhone       = request.RecipientPhone,
            ExpiresAtUtc         = preparation.CurrentExpiresAtUtc,
            DaysUntilExpiry      = daysUntilExpiry,
            RenewalPrice         = preparation.RenewalPrice,
            CurrencyCode         = preparation.CurrencyCode,
            IdempotencyKey       = idempotencyKey,
            RequestedByUserId    = dispatchedByUserId,
            RequestedAtUtc       = DateTimeOffset.UtcNow,
        }, ct);

        // ── Mark queued ───────────────────────────────────────────────────────
        preparation.MarkNotificationQueued(
            correlationId:       correlationId,
            channelsJson:        channelsJson,
            templateCode:        templateCode,
            languageCode:        languageCode,
            dispatchedByUserId:  dispatchedByUserId);

        await _preparations.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Renewal notification dispatched. PreparationId={Id} Code={Code} CorrelationId={Corr}",
            preparation.Id, preparation.RenewalCode, correlationId);

        var product = await _products.GetByCodeAsync(preparation.ProductCode, ct);
        return PrepareCargoDryKitRenewalCommandHandler.MapToDto(preparation, product?.Name);
    }
}
