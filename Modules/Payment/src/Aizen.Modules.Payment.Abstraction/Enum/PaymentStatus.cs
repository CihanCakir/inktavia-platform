namespace Aizen.Modules.Payment.Abstraction
// <summary>
// Represents the status of a payment in the Inktavia Store domain.
// </summary>
{
    public enum PaymentStatus
    {
        Pending = 1,
        Completed = 2,
        Refunded = 3,
        Failed = 4
    }
    public enum PayoutMethodStatus { None = 0, Pending = 1, Active = 2, Failed = 3 }
    public enum PaymentProfileStatus { Active = 1, OnHold = 2, Blocked = 3 }
}
