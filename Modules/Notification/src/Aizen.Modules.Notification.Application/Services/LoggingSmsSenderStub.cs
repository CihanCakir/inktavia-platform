using Microsoft.Extensions.Logging;

namespace Aizen.Modules.Notification.Application.Services;

/// <summary>
/// Vendor-siz SMS stub'ı (LoggingEmailSenderStub'ın aynısı): gönderimi loglar, "stub-" önekli bir ProviderRef döner.
/// Gerçek sağlayıcı adaptörü yokken sistem build/çalış/test edilebilsin diye. Provider="stub" iken bağlanır.
/// </summary>
public sealed class LoggingSmsSenderStub : ISmsSender
{
    private readonly ILogger<LoggingSmsSenderStub> _logger;

    public LoggingSmsSenderStub(ILogger<LoggingSmsSenderStub> logger) => _logger = logger;

    public Task<SmsSendResult> SendAsync(string e164Phone, string text, CancellationToken ct)
    {
        _logger.LogInformation("[SMS STUB] SMS to {To}: {Length} chars", PhoneMasker.Mask(e164Phone), text?.Length ?? 0);
        return Task.FromResult(SmsSendResult.Ok($"stub-{Guid.NewGuid()}"));
    }
}
