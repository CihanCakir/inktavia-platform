using Aizen.Bff.Marine.Participant.Mobile.Application.Common.RemoteClients;
using Aizen.Bff.Marine.Participant.Mobile.Application.Common.Services;
using Aizen.Bff.Marine.Participant.Mobile.Application.Contracts.Membership;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;
using Aizen.Core.Infrastructure.Exception;

namespace Aizen.Bff.Marine.Participant.Mobile.Application.Membership;

/// <summary>GET /api/v1/mobile/membership/subscription — the caller's current active subscription, or null (no plan).</summary>
public sealed class GetMobileCurrentSubscriptionQuery : AizenQuery<MobileCurrentSubscriptionDto> { }
