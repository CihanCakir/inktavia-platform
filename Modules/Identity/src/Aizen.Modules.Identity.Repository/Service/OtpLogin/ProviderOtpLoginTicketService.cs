using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Aizen.Core.Cache.Abstraction;
using Aizen.Modules.Identity.Domain.Interface.Service;
using Microsoft.Extensions.Options;

namespace Aizen.Modules.Identity.Repository.Identity.Service.OtpLogin;

public sealed class ProviderOtpLoginTicketService : IProviderOtpLoginTicketService
{
    private readonly IAizenDistributedCache _cache;
    private readonly OtpLoginTicketOptions _options;
    private const string CacheKeyPrefix = "otplogin:ticket:";

    public ProviderOtpLoginTicketService(IAizenDistributedCache cache, IOptions<OtpLoginTicketOptions> options)
    {
        _cache = cache;
        _options = options.Value;
    }

    public async Task<OtpLoginTicketResult> MintAsync(string keycloakSubjectId, string clientId, CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;
        var jti = Guid.NewGuid().ToString("N");
        var payload = new Dictionary<string, object>
        {
            ["sub"] = keycloakSubjectId,
            ["clientId"] = clientId,
            ["nonce"] = Guid.NewGuid().ToString("N"),
            ["iat"] = now.ToUnixTimeSeconds(),
            ["exp"] = now.ToUnixTimeSeconds() + _options.TtlSeconds,
            ["jti"] = jti,
        };

        var payloadJson = JsonSerializer.Serialize(payload);
        var payloadBytes = Encoding.UTF8.GetBytes(payloadJson);
        var secretBytes = Encoding.UTF8.GetBytes(_options.Secret);

        byte[] hmac;
        using (var h = new HMACSHA256(secretBytes))
            hmac = h.ComputeHash(payloadBytes);

        var ticket = Base64UrlEncode(payloadBytes) + "." + Base64UrlEncode(hmac);

        // Store jti → sub in Redis (single-use), TTL = ticket TTL
        await _cache.SetNoHash($"{CacheKeyPrefix}{jti}", keycloakSubjectId, TimeSpan.FromSeconds(_options.TtlSeconds));

        return new OtpLoginTicketResult { LoginTicket = ticket, ExpiresInSeconds = _options.TtlSeconds };
    }

    public async Task<OtpLoginConsumeResult> ConsumeAsync(string jti, CancellationToken ct)
    {
        var key = $"{CacheKeyPrefix}{jti}";
        try
        {
            var sub = await _cache.GetNoHash<string>(key);
            if (string.IsNullOrEmpty(sub))
                return new OtpLoginConsumeResult { Consumed = false };

            // Atomic delete — even if delete fails, the TTL will expire it
            await _cache.RemoveNoHash(key);
            return new OtpLoginConsumeResult { Consumed = true, Sub = sub };
        }
        catch
        {
            return new OtpLoginConsumeResult { Consumed = false };
        }
    }

    private static string Base64UrlEncode(byte[] input) =>
        Convert.ToBase64String(input).Replace('+', '-').Replace('/', '_').TrimEnd('=');
}
