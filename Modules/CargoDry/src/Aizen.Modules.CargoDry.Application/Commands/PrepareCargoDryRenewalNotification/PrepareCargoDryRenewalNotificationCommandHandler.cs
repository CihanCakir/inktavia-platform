using Aizen.Core.CQRS.Handler;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Core.InfoAccessor.Abstraction;
using Aizen.Modules.CargoDry.Abstraction.Dto;
using Aizen.Modules.CargoDry.Application.Commands.PrepareCargoDryKitRenewal;
using Aizen.Modules.CargoDry.Domain.Interface.Repository;
using Microsoft.Extensions.Logging;

namespace Aizen.Modules.CargoDry.Application.Commands.PrepareCargoDryRenewalNotification;

[DocumentationInfo("PrepareCargoDryRenewalNotificationCommandHandler",
    "Sets notification template, language, and channels on the preparation entity. " +
    "Review step before DispatchCargoDryRenewalNotification. " +
    "Does NOT publish any bus message. Does NOT call SMS/Mail/Push. " +
    "Phase 11 (July 2026).")]
public sealed class PrepareCargoDryRenewalNotificationCommandHandler
    : AizenCommandHandler<PrepareCargoDryRenewalNotificationCommand, CargoDryRenewalPreparationDto>
{
    private readonly ICargoDryRenewalPreparationRepository _preparations;
    private readonly ICargoDryProductRepository            _products;
    private readonly IAizenInfoAccessor                    _info;
    private readonly ILogger<PrepareCargoDryRenewalNotificationCommandHandler> _logger;

    public PrepareCargoDryRenewalNotificationCommandHandler(
        ICargoDryRenewalPreparationRepository                    preparations,
        ICargoDryProductRepository                               products,
        IAizenInfoAccessor                                       info,
        ILogger<PrepareCargoDryRenewalNotificationCommandHandler> logger)
    {
        _preparations = preparations;
        _products     = products;
        _info         = info;
        _logger       = logger;
    }

    public override async Task<CargoDryRenewalPreparationDto> Handle(
        PrepareCargoDryRenewalNotificationCommand request, CancellationToken ct)
    {
        var preparation = await _preparations.GetByIdAsync(request.RenewalPreparationId, ct)
            ?? throw new AizenBusinessException(
                $"Renewal preparation {request.RenewalPreparationId} not found.");

        if (preparation.IsTerminal)
            throw new AizenBusinessException(
                $"Renewal preparation {preparation.Id} ({preparation.RenewalCode}) is in terminal status " +
                $"{preparation.Status} — cannot configure notification.");

        if (!preparation.CanDispatchNotification)
            throw new AizenBusinessException(
                $"Renewal preparation {preparation.Id} ({preparation.RenewalCode}) cannot dispatch notification: " +
                "kit has no owner (OwnerUserId is null).");

        preparation.MarkNotificationPrepared(
            templateCode:     request.TemplateCode,
            languageCode:     request.LanguageCode,
            channelsJson:     request.ChannelsJson,
            preparedByUserId: _info.UserInfoAccessor.UserInfo.UserId);

        await _preparations.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Renewal notification prepared. PreparationId={Id} Code={Code} Template={Template} Channels={Channels}",
            preparation.Id, preparation.RenewalCode, request.TemplateCode, request.ChannelsJson);

        var product = await _products.GetByCodeAsync(preparation.ProductCode, ct);
        return PrepareCargoDryKitRenewalCommandHandler.MapToDto(preparation, product?.Name);
    }
}
