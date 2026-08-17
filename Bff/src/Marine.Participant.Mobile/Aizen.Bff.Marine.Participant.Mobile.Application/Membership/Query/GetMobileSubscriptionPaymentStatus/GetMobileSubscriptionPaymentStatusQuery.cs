using Aizen.Bff.Marine.Participant.Mobile.Application.Common.RemoteClients;
using Aizen.Bff.Marine.Participant.Mobile.Application.Common.Services;
using Aizen.Bff.Marine.Participant.Mobile.Application.Contracts.Membership;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;
using Aizen.Core.Infrastructure.Exception;

namespace Aizen.Bff.Marine.Participant.Mobile.Application.Membership;

/// <summary>GET /api/v1/mobile/membership/subscription/payment-status/{transactionId} — the paid-subscription
/// checkout's payment status (poll: Pending → Paid/Failed). Owner-scoped module-side. Cost-free.</summary>
public sealed class GetMobileSubscriptionPaymentStatusQuery : AizenQuery<MobileMembershipPaymentStatusDto>
{
    public GetMobileSubscriptionPaymentStatusQuery(long transactionId) => TransactionId = transactionId;
    public long TransactionId { get; }
}
