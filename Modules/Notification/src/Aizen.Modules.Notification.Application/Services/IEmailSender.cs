namespace Aizen.Modules.Notification.Application.Services;

public interface IEmailSender
{
    Task<string> SendAsync(string toEmail, string subject, string htmlBody, CancellationToken ct);
}
