using System.Text.Json.Serialization;

namespace Aizen.Modules.Identity.Abstraction.Enum
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum RegistrationStatus
    {
        Active = 1,
        PendingApproval = 2,
        EmailVerificationRequired = 3,
        PhoneVerificationRequired = 4,
        Rejected = 5
    }
}