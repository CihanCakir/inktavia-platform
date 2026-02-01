namespace Aizen.Modules.Payment.Abstraction.Request
{
    public sealed class InitiatePaymentForInvitationJoinRequest
    {
        public long ActivityId { get; init; }
        public long ParticipantProfileId { get; init; }
        public string InvitationCode { get; init; } = default!;
        public long? ScheduleId { get; init; }
        public string Currency { get; init; } = "TRY";
        public string Provider { get; init; } = "iyzico";
    }
}