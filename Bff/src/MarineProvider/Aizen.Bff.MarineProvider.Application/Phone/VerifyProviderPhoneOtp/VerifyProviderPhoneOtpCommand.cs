using Aizen.Bff.MarineProvider.Application.Contracts.Phone;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.MarineProvider.Application.Phone.VerifyProviderPhoneOtp;

/// <summary>
/// Verify a phone OTP code and persist phoneVerified on the resolved Organizer profile.
/// Not a login method; no Keycloak SMS OTP.
/// </summary>
public sealed class VerifyProviderPhoneOtpCommand : AizenCommand<VerifyProviderPhoneOtpResponse>
{
    public string PhoneNumber { get; set; } = default!;
    public int Otp { get; set; }
    public string ValidationGuid { get; set; } = default!;
}
