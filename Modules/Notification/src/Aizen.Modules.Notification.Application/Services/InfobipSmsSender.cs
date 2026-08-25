using System.Text;
using System.Text.Json;
using Aizen.Core.Common.Abstraction.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Aizen.Modules.Notification.Application.Services;

/// <summary>
/// Infobip SMS adaptörü: POST {BaseUrl}/sms/2/text/advanced, header "Authorization: App {ApiKey}",
/// gövde { messages:[{ destinations:[{ to }], from, text }] }. Yanıttan messageId → ProviderRef. Non-2xx ya da
/// reddedilen durum → başarısızlık (yanıt gövdesi özetiyle). IHttpClientFactory kullanır.
/// NOT: Infobip trial hesabı yalnız doğrulanmış numaraya gönderir — dev testleri için yeterli.
/// </summary>
public sealed class InfobipSmsSender : ISmsSender
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly SmsOptions _options;
    private readonly ILogger<InfobipSmsSender> _logger;

    public InfobipSmsSender(
        IHttpClientFactory httpClientFactory, IOptions<SmsOptions> options, ILogger<InfobipSmsSender> logger)
    {
        _httpClientFactory = httpClientFactory;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<SmsSendResult> SendAsync(string e164Phone, string text, CancellationToken ct)
    {
        var cfg = _options.Infobip;
        var baseUrl = AizenConfigPlaceholders.NullIfUnset(cfg.BaseUrl);
        var apiKey  = AizenConfigPlaceholders.NullIfUnset(cfg.ApiKey);
        var from    = AizenConfigPlaceholders.NullIfUnset(cfg.From);
        if (baseUrl is null || apiKey is null)
            return SmsSendResult.Fail("Infobip yapılandırması eksik (BaseUrl/ApiKey).");

        var payload = new
        {
            messages = new[]
            {
                new { destinations = new[] { new { to = e164Phone } }, from, text },
            },
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl.TrimEnd('/')}/sms/2/text/advanced")
        {
            Content = new StringContent(JsonSerializer.Serialize(payload, Json), Encoding.UTF8, "application/json"),
        };
        request.Headers.TryAddWithoutValidation("Authorization", $"App {apiKey}");
        request.Headers.TryAddWithoutValidation("Accept", "application/json");

        try
        {
            var client = _httpClientFactory.CreateClient(nameof(InfobipSmsSender));
            var response = await client.SendAsync(request, ct);
            var raw = await response.Content.ReadAsStringAsync(ct);

            if (!response.IsSuccessStatusCode)
                return SmsSendResult.Fail($"Infobip HTTP {(int)response.StatusCode}: {Summarize(raw)}");

            var parsed = JsonSerializer.Deserialize<InfobipResponse>(raw, Json);
            var message = parsed?.Messages?.FirstOrDefault();
            if (message?.MessageId is null)
                return SmsSendResult.Fail($"Infobip yanıtında messageId yok: {Summarize(raw)}");

            // Gönderim anında durum genelde PENDING olur; REJECTED/UNDELIVERABLE ise başarısızlık.
            var group = message.Status?.GroupName?.ToUpperInvariant();
            if (group is "REJECTED" or "UNDELIVERABLE")
                return SmsSendResult.Fail($"Infobip {group}: {message.Status?.Description}");

            return SmsSendResult.Ok(message.MessageId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Infobip SMS request failed for {To}", e164Phone);
            return SmsSendResult.Fail($"Infobip isteği başarısız: {ex.Message}");
        }
    }

    private static string Summarize(string? body)
    {
        if (string.IsNullOrWhiteSpace(body)) return "(boş yanıt)";
        var trimmed = body.Trim();
        return trimmed.Length <= 300 ? trimmed : trimmed[..300] + "…";
    }

    // Infobip /sms/2/text/advanced yanıt şekli (yalnız gerekli alanlar; Web JSON = case-insensitive).
    private sealed class InfobipResponse
    {
        public List<InfobipMessage>? Messages { get; set; }
    }

    private sealed class InfobipMessage
    {
        public string? MessageId { get; set; }
        public InfobipStatus? Status { get; set; }
    }

    private sealed class InfobipStatus
    {
        public int GroupId { get; set; }
        public string? GroupName { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
    }
}
