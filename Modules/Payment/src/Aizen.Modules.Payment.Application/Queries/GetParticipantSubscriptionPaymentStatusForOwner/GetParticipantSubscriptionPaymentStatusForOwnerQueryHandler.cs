using Aizen.Core.CQRS.Handler;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.Payment.Abstraction.Model.Result;
using Aizen.Modules.Payment.Domain.Interface.Repository;

namespace Aizen.Modules.Payment.Application.Queries.GetParticipantSubscriptionPaymentStatusForOwner;

/// <summary>
/// BE-MO7 — reads the payment status of a participant-subscription checkout for the calling participant. Owner-gates:
/// the transaction must exist, be a Subscription-context transaction, and be paid by this participant — otherwise a
/// clean not-found. Maps the raw <see cref="PaymentTransactionStatus"/> to the same owner lifecycle the MO3 poll uses
/// (None / Pending / Paid / Failed / Cancelled). Cost-free: customer amount + status + timestamp only.
/// </summary>
public sealed class GetParticipantSubscriptionPaymentStatusForOwnerQueryHandler
    : AizenQueryHandler<GetParticipantSubscriptionPaymentStatusForOwnerQuery, ParticipantSubscriptionPaymentStatusResult>
{
    private readonly IPaymentTransactionRepository _transactions;

    public GetParticipantSubscriptionPaymentStatusForOwnerQueryHandler(IPaymentTransactionRepository transactions)
        => _transactions = transactions;

    public override async Task<ParticipantSubscriptionPaymentStatusResult?> Handle(
        GetParticipantSubscriptionPaymentStatusForOwnerQuery request, CancellationToken ct)
    {
        var tx = await _transactions.GetByIdAsync(request.TransactionId, ct);

        // Owner gate: the transaction must be this participant's own subscription checkout.
        if (tx is null
            || tx.ContextType != TransactionContextType.Subscription
            || tx.PayerProfileId != request.ParticipantProfileId)
            throw new AizenBusinessException("Subscription payment not found.");

        return new ParticipantSubscriptionPaymentStatusResult(
            HasPayment:    true,
            TransactionId: tx.Id,
            Status:        MapLifecycle(tx.Status),
            RawStatus:     tx.Status.ToString(),
            Amount:        tx.GrossAmount,
            CurrencyCode:  tx.CurrencyCode,
            PaidAt:        tx.CapturedAt);
    }

    // Mirrors the MO3 owner lifecycle map: "Paid" once a capture happened.
    private static string MapLifecycle(PaymentTransactionStatus s) => s switch
    {
        PaymentTransactionStatus.PendingIntent => "Pending",
        PaymentTransactionStatus.Failed        => "Failed",
        PaymentTransactionStatus.Cancelled     => "Cancelled",
        _                                       => "Paid",
    };
}
