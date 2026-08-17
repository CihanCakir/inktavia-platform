using Aizen.Bff.MarineProvider.Application.Common.Warnings;

namespace Aizen.Bff.MarineProvider.Application.Contracts.Phone;

public sealed class SendProviderPhoneOtpResponse
{
    public string? ValidationGuid { get; set; }
    public DateTime? ExpiredDateTime { get; set; }
    public int ExpireCounter { get; set; }
    public string Message { get; set; } = string.Empty;
    public List<ProviderBffWarning> Warnings { get; set; } = new();
}
