using Aizen.Bff.Marine.Participant.Mobile.Application.Contracts.Membership;
using Aizen.Modules.Payment.Abstraction.Model.Result;

namespace Aizen.Bff.Marine.Participant.Mobile.Application.Membership;

/// <summary>
/// Projects the Payment participant-membership results → the cost-free mobile contracts (BE_MO7). Only customer-facing
/// figures cross (plan price, the discount rate/perks, the amount the owner paid); no platform/provider funding
/// internals. Enums map to their string names.
/// </summary>
internal static class MobileMembershipMapper
{
    public static MobileCurrentSubscriptionDto MapSubscription(ActiveParticipantSubscriptionResult s) => new()
    {
        SubscriptionId      = s.SubscriptionId,
        ParticipantPlanId   = s.ParticipantPlanId,
        PlanCode            = s.PlanCode,
        Status              = s.Status.ToString(),
        PaidAmount          = s.PaidAmount,
        CurrencyCode        = s.CurrencyCode,
        PeriodStart         = s.PeriodStart,
        PeriodEnd           = s.PeriodEnd,
        AutoRenew           = s.AutoRenew,
        ServiceDiscountRate = s.ServiceDiscountAtSubscription,
        EarnMultiplier      = s.EarnMultiplierAtSubscription,
    };

    public static MobileMembershipPaymentStatusDto MapPaymentStatus(ParticipantSubscriptionPaymentStatusResult? p)
    {
        if (p is null)
            return new MobileMembershipPaymentStatusDto { HasPayment = false, Status = "None" };

        return new MobileMembershipPaymentStatusDto
        {
            HasPayment    = p.HasPayment,
            TransactionId = p.TransactionId,
            Status        = p.Status,
            Amount        = p.Amount,
            CurrencyCode  = p.CurrencyCode,
            PaidAt        = p.PaidAt,
        };
    }

    public static MobileSubscribeResultDto MapSubscribeResult(
        SubscribeParticipantForOwnerResult r, MobileMembershipPaymentStatusDto payment) => new()
    {
        Mode              = r.Mode,
        ParticipantPlanId = r.ParticipantPlanId,
        PlanCode          = r.PlanCode,
        PlanName          = r.PlanName,
        Amount            = r.Amount,
        CurrencyCode      = r.CurrencyCode,
        SubscriptionId    = r.SubscriptionId,
        TransactionId     = r.TransactionId,
        Payment           = payment,
    };

    public static MobileCancelSubscriptionDto MapCancel(CancelSubscriptionResult r) => new()
    {
        SubscriptionId = r.SubscriptionId,
        Status         = r.Status,
        CancelledAt    = r.CancelledAt,
    };
}
