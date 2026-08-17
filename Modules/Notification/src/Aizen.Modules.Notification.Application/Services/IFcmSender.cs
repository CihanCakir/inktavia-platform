namespace Aizen.Modules.Notification.Application.Services;

public interface IFcmSender
{
    Task<string> SendAsync(string deviceToken, string title, string body, string? dataJson, CancellationToken ct);
}
