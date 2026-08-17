using Aizen.Bff.MarineProvider.Application.Common.Warnings;

namespace Aizen.Bff.MarineProvider.Application.Contracts.Phone;

public sealed class VerifyProviderPhoneOtpResponse
{
    public bool IsConfirmed { get; set; }
    public bool PhoneVerifiedPersisted { get; set; }
    public string Message { get; set; } = string.Empty;
    public List<ProviderBffWarning> Warnings { get; set; } = new();
}
