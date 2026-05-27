namespace Aizen.Modules.Payment.Abstraction.Request
{
    public sealed class InitiatePaymentForJoinRequestRequest
    {
        public long ActivityId { get; init; }
        public long? ScheduleId { get; init; }
        public long ParticipantProfileId { get; init; }
        public string Currency { get; init; } = "TRY";
        public string Provider { get; init; } = "iyzico"; // veya "stripe"
    }
}