using Microsoft.Extensions.Logging;

namespace Aizen.Modules.Notification.Application.Services;

public sealed class FcmSenderStub : IFcmSender
{
    private readonly ILogger<FcmSenderStub> _logger;

    public FcmSenderStub(ILogger<FcmSenderStub> logger) => _logger = logger;

    public Task<string> SendAsync(
        string deviceToken, string title, string body,
        string? dataJson, CancellationToken ct)
    {
        _logger.LogInformation(
            "[FCM STUB] Push to {Token}: Title={Title}",
            deviceToken[..Math.Min(10, deviceToken.Length)] + "...", title);
        return Task.FromResult($"stub-{Guid.NewGuid()}");
    }
}
