using Aizen.Core.CQRS.Message;
using Aizen.Modules.CargoDry.Abstraction.Dto;

namespace Aizen.Modules.CargoDry.Application.Commands.PrepareCargoDryRenewalNotification;

/// <summary>
/// Sets the notification template, language, and target channels on an existing renewal preparation.
/// Does NOT publish any message. Does NOT call SMS/Mail/Push providers.
/// Phase 11 (July 2026).
/// </summary>
[DocumentationInfo("Prepare CargoDry renewal notification command",
    "Sets notification template + channels on the preparation record. " +
    "This is the review step before dispatch. Does NOT publish a message. " +
    "Phase 11 (July 2026).")]
public sealed class PrepareCargoDryRenewalNotificationCommand
    : AizenCommand<CargoDryRenewalPreparationDto>
{
    public required long    RenewalPreparationId { get; init; }
    public required string  TemplateCode         { get; init; }
    public required string  LanguageCode         { get; init; }
    /// <summary>JSON array of channel names: ["Email","Sms","InApp"]</summary>
    public required string  ChannelsJson         { get; init; }
}
