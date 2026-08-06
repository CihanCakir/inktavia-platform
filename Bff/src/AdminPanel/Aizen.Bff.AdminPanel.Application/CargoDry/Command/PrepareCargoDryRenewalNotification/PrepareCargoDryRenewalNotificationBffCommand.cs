using Aizen.Bff.AdminPanel.Application.CargoDry.Dto;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.CargoDry.Command.PrepareCargoDryRenewalNotification;

[DocumentationInfo("Prepare CargoDry renewal notification BFF command",
    "Stamps notification metadata (template, language, channels) on the renewal preparation. " +
    "Does NOT dispatch the notification. Phase 11 (July 2026).")]
public sealed class PrepareCargoDryRenewalNotificationBffCommand
    : AizenCommand<PrepareCargoDryRenewalNotificationBffCommandResponse>
{
    public required long    RenewalPreparationId { get; init; }
    public required string  TemplateCode         { get; init; } = default!;
    public string           LanguageCode         { get; init; } = "tr";
    public string           ChannelsJson         { get; init; } = "[\"Email\"]";
}

public sealed class PrepareCargoDryRenewalNotificationBffCommandResponse
{
    public CargoDryRenewalPreparationBffDto? Preparation { get; init; }
}
