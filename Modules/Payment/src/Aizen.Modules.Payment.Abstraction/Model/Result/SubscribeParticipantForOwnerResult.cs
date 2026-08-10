namespace Aizen.Modules.Payment.Abstraction.Model.Result;

/// <summary>
/// BE-MO7 — the outcome of an owner subscribing to a participant plan. A FREE/launch plan is applied immediately
/// (<see cref="Mode"/> = <c>Immediate</c>, the subscription exists now). A PAID plan is iyzico-gated: a PendingIntent
/// checkout transaction is created (<see cref="Mode"/> = <c>PaymentPending</c>, <see cref="TransactionId"/> set) and
/// the subscription is created by the existing capture consumer once the payment is captured (dev: manual-gateway
/// capture; live: iyzico webhook — reuses the MO3 capture→consumer path). Cost-free: the plan's customer price only.
/// </summary>
public sealed record SubscribeParticipantForOwnerResult(
    string    Mode,                 // "Immediate" (free applied now) | "PaymentPending" (paid, awaiting capture)
    long      ParticipantPlanId,
    string    PlanCode,
    string    PlanName,
    decimal   Amount,               // the plan's customer price (server-resolved, never client-supplied)
    string    CurrencyCode,
    long?     SubscriptionId,        // set when Immediate (the subscription already exists)
    long?     TransactionId,         // set when PaymentPending (poll this via the payment-status endpoint)
    string?   CheckoutFormContent,   // iyzico form (null on the manual/dev gateway)
    string?   RedirectUrl);
