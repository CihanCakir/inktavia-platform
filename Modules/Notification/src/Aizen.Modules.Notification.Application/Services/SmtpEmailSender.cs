using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Aizen.Modules.Notification.Application.Services;

public sealed class SmtpEmailSender : IEmailSender
{
    private readonly EmailOptions _options;
    private readonly ILogger<SmtpEmailSender> _logger;

    public SmtpEmailSender(IOptions<EmailOptions> options, ILogger<SmtpEmailSender> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task<string> SendAsync(string toEmail, string subject, string htmlBody, CancellationToken ct)
    {
        using var client = new SmtpClient(_options.Host, _options.Port)
        {
            Credentials = new NetworkCredential(_options.User, _options.Password),
            EnableSsl = _options.UseSsl,
        };

        var message = new MailMessage
        {
            From = new MailAddress(_options.FromAddress, _options.FromName),
            Subject = subject,
            Body = htmlBody,
            IsBodyHtml = true,
        };
        message.To.Add(toEmail);

        // From no-reply@... olduğundan yanıtlar izlenen bir kutuya gitsin (aksi halde bounce).
        if (!string.IsNullOrWhiteSpace(_options.ReplyTo))
            message.ReplyToList.Add(new MailAddress(_options.ReplyTo));

        await client.SendMailAsync(message, ct);

        var messageId = Guid.NewGuid().ToString();
        _logger.LogInformation("Email sent to {To} subject={Subject} ref={Ref}", toEmail, subject, messageId);
        return messageId;
    }
}
