using Aizen.Bff.AdminPanel.Application.AdminCargoDry.Dto;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.AdminCargoDry.Command.DispatchCargoDryRenewalNotification;

[DocumentationInfo("Dispatch CargoDry renewal notification BFF command",
    "Publishes CargoDryRenewalNotificationRequestedMessage via the message bus. " +
    "Actual delivery is owned by the Notification module. Hard rules #2, #4, #13, #14, #15. " +
    "Phase 11 (July 2026).")]
public sealed class DispatchCargoDryRenewalNotificationBffCommand
    : AizenCommand<DispatchCargoDryRenewalNotificationBffCommandResponse>
{
    public required long    RenewalPreparationId { get; init; }
    public string?          RecipientEmail       { get; init; }
    public string?          RecipientPhone       { get; init; }
}

public sealed class DispatchCargoDryRenewalNotificationBffCommandResponse
{
    public CargoDryRenewalPreparationBffDto? Preparation { get; init; }
}
