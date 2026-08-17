using Aizen.Bff.Marine.Participant.Mobile.Application.Common.RemoteClients;
using Aizen.Bff.Marine.Participant.Mobile.Application.Common.Services;
using Aizen.Bff.Marine.Participant.Mobile.Application.Contracts.Notification;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;
using Aizen.Core.Infrastructure.Exception;

namespace Aizen.Bff.Marine.Participant.Mobile.Application.Notification;

/// <summary>POST /api/v1/mobile/notifications/mark-all-read — mark all of the caller's notifications read.</summary>
public sealed class MarkAllMobileNotificationsReadCommand : AizenCommand<MobileMarkAllReadResultDto> { }
