namespace Aizen.Modules.Payment.Abstraction.Model.Request;

public sealed class SubscribeParticipantPlanRequest
{
    public required long     ParticipantProfileId  { get; init; }
    public required long     ParticipantPlanId     { get; init; }
    public required decimal  PaidAmount            { get; init; }
    public required string   CurrencyCode          { get; init; }
    public required DateTime PeriodStart           { get; init; }
    public required DateTime PeriodEnd             { get; init; }
    public          bool     AutoRenew             { get; init; } = false;
    public          long?    PaymentTransactionId  { get; init; }
}
