using Aizen.Bff.Marine.Participant.Mobile.Application.Common.RemoteClients;
using Aizen.Bff.Marine.Participant.Mobile.Application.Common.Services;
using Aizen.Bff.Marine.Participant.Mobile.Application.Contracts.Notification;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.Notification.Abstraction.Request;

namespace Aizen.Bff.Marine.Participant.Mobile.Application.Notification;

/// <summary>GET /api/v1/mobile/notifications/preferences — the caller's per-category × channel preference matrix.</summary>
public sealed class GetMobileNotificationPreferencesQuery : AizenQuery<MobileNotificationPreferencesDto> { }
