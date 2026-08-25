using System.Text;
using System.Text.Json;
using Aizen.Core.Common.Abstraction.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Aizen.Modules.Notification.Application.Services;

/// <summary>
/// Netgsm SMS adaptörü: Basic auth (UserCode/Password), MsgHeader = onaylı gönderen başlığı. ProviderRef = dönen job id.
/// IHttpClientFactory kullanır.
/// </summary>
public sealed class NetgsmSmsSender : ISmsSender
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly SmsOptions _options;
    private readonly ILogger<NetgsmSmsSender> _logger;

    public NetgsmSmsSender(
        IHttpClientFactory httpClientFactory, IOptions<SmsOptions> options, ILogger<NetgsmSmsSender> logger)
    {
        _httpClientFactory = httpClientFactory;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<SmsSendResult> SendAsync(string e164Phone, string text, CancellationToken ct)
    {
        var cfg = _options.Netgsm;
        var baseUrl  = AizenConfigPlaceholders.NullIfUnset(cfg.BaseUrl) ?? "https://api.netgsm.com.tr";
        var userCode = AizenConfigPlaceholders.NullIfUnset(cfg.UserCode);
        var password = AizenConfigPlaceholders.NullIfUnset(cfg.Password);
        var header   = AizenConfigPlaceholders.NullIfUnset(cfg.MsgHeader);
        if (userCode is null || password is null)
            return SmsSendResult.Fail("Netgsm yapılandırması eksik (UserCode/Password).");

        try
        {
            var client = _httpClientFactory.CreateClient(nameof(NetgsmSmsSender));

            // ⚠️ TEK YER: Netgsm istek yolu + gövde + yanıt-kodu eşlemesi burada toplandı. Netgsm'in GÜNCEL REST
            //    (v2) dokümanına göre yazıldı; ÜRETİM ÖNCESİ Netgsm güncel dokümanıyla DOĞRULANMALI (hesap/başlık
            //    henüz onay sürecinde). Numara '+' olmadan gönderilir; kod "00" = kabul, jobid döner.
            var no = e164Phone.TrimStart('+');
            var payload = new
            {
                msgheader = header,
                encoding  = "TR",
                messages  = new[] { new { msg = text, no } },
            };

            using var request = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl.TrimEnd('/')}/sms/rest/v2/send")
            {
                Content = new StringContent(JsonSerializer.Serialize(payload, Json), Encoding.UTF8, "application/json"),
            };
            var basic = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{userCode}:{password}"));
            request.Headers.TryAddWithoutValidation("Authorization", $"Basic {basic}");
            request.Headers.TryAddWithoutValidation("Accept", "application/json");

            var response = await client.SendAsync(request, ct);
            var raw = await response.Content.ReadAsStringAsync(ct);

            if (!response.IsSuccessStatusCode)
                return SmsSendResult.Fail($"Netgsm HTTP {(int)response.StatusCode}: {Summarize(raw)}");

            var parsed = JsonSerializer.Deserialize<NetgsmResponse>(raw, Json);
            return MapResult(parsed);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Netgsm SMS request failed for {To}", PhoneMasker.Mask(e164Phone));
            return SmsSendResult.Fail($"Netgsm isteği başarısız: {ex.Message}");
        }
    }

    // Netgsm yanıt-kodu eşlemesi: "00" (jobid ile) = kabul; diğerleri hata (bilinenler Türkçeleştirildi).
    private static SmsSendResult MapResult(NetgsmResponse? r)
    {
        var code = r?.Code;
        if (code == "00" && !string.IsNullOrWhiteSpace(r?.JobId))
            return SmsSendResult.Ok(r!.JobId!);

        var message = code switch
        {
            "20" => "mesaj metni/karakter sayısı hatası",
            "30" => "geçersiz kullanıcı adı/şifre ya da API erişimi kapalı",
            "40" => "mesaj başlığı (sender id) onaysız",
            "50" => "İYS kaynaklı gönderilemedi",
            "51" => "abonelik başlangıç tarihi hatası",
            "70" => "hatalı sorgu/parametre",
            _    => $"Netgsm kodu {code ?? "(yok)"}",
        };
        if (!string.IsNullOrWhiteSpace(r?.Description))
            message += $" — {r!.Description}";
        return SmsSendResult.Fail(message);
    }

    private static string Summarize(string? body)
    {
        if (string.IsNullOrWhiteSpace(body)) return "(boş yanıt)";
        var trimmed = body.Trim();
        return trimmed.Length <= 300 ? trimmed : trimmed[..300] + "…";
    }

    // Netgsm REST v2 yanıt şekli (Web JSON = case-insensitive; "jobid"→JobId).
    private sealed class NetgsmResponse
    {
        public string? Code { get; set; }
        public string? JobId { get; set; }
        public string? Description { get; set; }
    }
}
