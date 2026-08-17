using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Aizen.Modules.Payment.Abstraction.Enum;

namespace Aizen.Modules.Payment.Abstraction.Model
{
    public sealed class TransactionContext
    {
        public TransactionContextType Type { get; init; }
        public TransactionContextSubType SubType { get; init; }
        public long GeneralId { get; init; }        // zorunlu
        public long? SubId { get; init; }           // opsiyonel
        public System.Text.Json.JsonDocument? Metadata { get; init; }

        // Shortcuts
        public static TransactionContext ForActivity(long activityId) =>
            new() { Type = TransactionContextType.Activity, SubType = TransactionContextSubType.None, GeneralId = activityId };

        public static TransactionContext ForActivitySchedule(long activityId, long scheduleId) =>
            new() { Type = TransactionContextType.Activity, SubType = TransactionContextSubType.Schedule, GeneralId = activityId, SubId = scheduleId };

        public static TransactionContext ForActivityInvitation(long activityId, long invitationId, long? scheduleId = null) =>
            new() { Type = TransactionContextType.Activity, SubType = TransactionContextSubType.Invitation, GeneralId = activityId, SubId = scheduleId ?? invitationId };

        public static TransactionContext ForProduct(long productId, long? variantId = null) =>
            new() { Type = TransactionContextType.Product, SubType = variantId.HasValue ? TransactionContextSubType.Variant : TransactionContextSubType.None, GeneralId = productId, SubId = variantId };

        public static TransactionContext ForSubscription(long subscriptionId, long? planTierId = null) =>
            new() { Type = TransactionContextType.Subscription, SubType = planTierId.HasValue ? TransactionContextSubType.PlanTier : TransactionContextSubType.None, GeneralId = subscriptionId, SubId = planTierId };

        // ── Marine OS factories ───────────────────────────────────────────────
        public static TransactionContext ForServiceRequest(long serviceRequestId, long offerId) =>
            new() { Type = TransactionContextType.ServiceRequest, SubType = TransactionContextSubType.Offer, GeneralId = serviceRequestId, SubId = offerId };

        public static TransactionContext ForCargoDry(long kitId) =>
            new() { Type = TransactionContextType.CargoDry, SubType = TransactionContextSubType.Kit, GeneralId = kitId };

        // ── Premium (BE-P11) — offer-scoped boost purchase ────────────────────
        public static TransactionContext ForPremiumOffer(long offerId) =>
            new() { Type = TransactionContextType.Premium, SubType = TransactionContextSubType.Offer, GeneralId = offerId };

        public static TransactionContext ForProviderSubscription(long providerProfileId, long planId) =>
            new() { Type = TransactionContextType.Subscription, SubType = TransactionContextSubType.PlanTier, GeneralId = providerProfileId, SubId = planId };

        public static TransactionContext ForParticipantSubscription(long participantProfileId, long planId) =>
            new() { Type = TransactionContextType.Subscription, SubType = TransactionContextSubType.PlanTier, GeneralId = participantProfileId, SubId = planId };
    }
}