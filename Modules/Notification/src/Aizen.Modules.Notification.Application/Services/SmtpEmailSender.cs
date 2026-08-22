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

        // FAZ15/16 (#32) — Message-ID'yi gönderimden ÖNCE, standart <local@domain> biçiminde BİZ üretiyoruz ve hem
        // başlığa hem de dönüş değerine AYNI değeri koyuyoruz. .NET SmtpClient sunucunun ürettiği id'yi geri vermez;
        // eski kod send'DEN SONRA rastgele bir Guid uyduruyordu (#32) — hiçbir yerde karşılığı olmayan, iz sürülemez
        // bir değer. Bu id İNŞA YOLUYLA bizimdir. Resend panosunda aynı e-postada görünmesi BEKLENİR — fakat HENÜZ
        // ÖLÇÜLMEDİ (FAZ16 Task C): SMTP relay istemci Message-ID'sini değiştirebilir ya da .NET SmtpClient kendi
        // başlığını ekleyip çift başlık üretebilir. Doğrulama: teshis/eposta-zinciri-dogrula.sh. İd'ler tutmuyorsa
        // çözüm Resend HTTP API'sidir (ayrı faz). Domain, From adresinin host'undan alınır.
        var domain = new MailAddress(_options.FromAddress).Host;
        var messageId = $"<{Guid.NewGuid():N}@{domain}>";
        message.Headers["Message-ID"] = messageId;

        await client.SendMailAsync(message, ct);

        _logger.LogInformation("Email sent to {To} subject={Subject} ref={Ref}", toEmail, subject, messageId);
        return messageId;
    }
}
