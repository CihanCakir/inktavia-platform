namespace Aizen.Modules.Payment.Abstraction
// <summary>
// Represents the reason for a refund in the Inktavia Store domain.
// </summary>
{
    public enum RefundReason
    {
        UserCancel = 1,
        OrganizerCancel = 2,
        SystemError = 3
    }
}
