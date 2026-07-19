using Aizen.Bff.MarineProvider.Application.Contracts.Phone;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.MarineProvider.Application.Phone;

/// <summary>
/// Send an optional post-registration phone OTP (Identity SMS OTP). Not a login method;
/// no Keycloak SMS OTP.
/// </summary>
public sealed class SendProviderPhoneOtpCommand : AizenCommand<SendProviderPhoneOtpResponse>
{
    public string PhoneNumber { get; set; } = default!;
}
