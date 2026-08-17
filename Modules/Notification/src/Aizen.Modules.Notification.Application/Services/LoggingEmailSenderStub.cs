using Microsoft.Extensions.Logging;

namespace Aizen.Modules.Notification.Application.Services;

public sealed class LoggingEmailSenderStub : IEmailSender
{
    private readonly ILogger<LoggingEmailSenderStub> _logger;

    public LoggingEmailSenderStub(ILogger<LoggingEmailSenderStub> logger) => _logger = logger;

    public Task<string> SendAsync(string toEmail, string subject, string htmlBody, CancellationToken ct)
    {
        _logger.LogInformation(
            "[EMAIL STUB] Email to {To}: Subject={Subject}",
            toEmail, subject);
        return Task.FromResult($"stub-{Guid.NewGuid()}");
    }
}
