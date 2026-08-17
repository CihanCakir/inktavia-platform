using Aizen.Bff.Marine.Participant.Mobile.Application.Common.RemoteClients;
using Aizen.Bff.Marine.Participant.Mobile.Application.Common.Services;
using Aizen.Bff.Marine.Participant.Mobile.Application.Contracts.Notification;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.Notification.Abstraction.Request;

namespace Aizen.Bff.Marine.Participant.Mobile.Application.Notification;

/// <summary>PUT /api/v1/mobile/notifications/preferences — toggle one category×channel cell (only Push/Email are
/// user-changeable; the module rejects locked cells). Returns the refreshed matrix.</summary>
public sealed class UpdateMobileNotificationPreferenceCommand : AizenCommand<MobileNotificationPreferencesDto>
{
    public UpdateMobileNotificationPreferenceCommand(MobileUpdatePreferenceRequest request) => Request = request;
    public MobileUpdatePreferenceRequest Request { get; }
}
