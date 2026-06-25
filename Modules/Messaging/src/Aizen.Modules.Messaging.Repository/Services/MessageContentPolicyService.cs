using System.Text.Json;
using System.Text.RegularExpressions;
using Aizen.Core.Cache.Abstraction;
using Aizen.Core.Cache.Abstraction.Common;
using Aizen.Modules.Messaging.Abstraction.Enum;
using Aizen.Modules.Messaging.Domain.Interface;

namespace Aizen.Modules.Messaging.Repository.Services;

[DocumentationInfo("Message content policy service",
    "Enforces platform content rules: blocks off-platform solicitation, personal data leakage, spam, and prohibited content.")]
public sealed class MessageContentPolicyService : IMessageContentPolicy
{
    private readonly IAizenCache _cache;

    private static readonly string[] OffPlatformKeywords =
    [
        "iban", "swift", "bic", "banka hesabı", "hesap no", "hesap numarası",
        "whatsapp", "telegram", "signal", "direct payment", "outside platform",
        "platform dışı", "direkt öde", "nakit öde", "cash payment"
    ];

    private static readonly Regex PhoneRegex =
        new(@"(\+?\d[\d\s\-\(\)]{8,}\d)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex EmailRegex =
        new(@"\b[A-Za-z0-9._%+\-]+@[A-Za-z0-9.\-]+\.[A-Za-z]{2,}\b", RegexOptions.Compiled);

    private static readonly Regex UrlRegex =
        new(@"https?://[^\s]+", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public MessageContentPolicyService(IAizenCache cache) => _cache = cache;

    public async Task<ContentPolicyResult> EvaluateAsync(
        string content, long senderUserId, MessageType messageType = MessageType.Text, CancellationToken ct = default)
    {
        // Location messages carry system-generated JSON — only validate structure
        if (messageType == MessageType.Location)
        {
            try { JsonDocument.Parse(content); }
            catch { return ContentPolicyResult.Block("Invalid location payload.", "INVALID_LOCATION"); }
            return ContentPolicyResult.Allow();
        }

        // 1. Content too long
        if (content.Length > 4000)
            return ContentPolicyResult.Block("Message exceeds maximum length of 4000 characters.", "CONTENT_TOO_LONG");

        var normalized = content.ToLowerInvariant();

        // 2. Off-platform solicitation — hard block
        foreach (var keyword in OffPlatformKeywords)
        {
            if (normalized.Contains(keyword))
                return ContentPolicyResult.Block(
                    $"Message contains off-platform solicitation keyword: '{keyword}'",
                    "OFF_PLATFORM_SOLICITATION");
        }

        // 3. Phone number leakage — soft flag, human review
        if (PhoneRegex.IsMatch(content))
            return ContentPolicyResult.Review("Message may contain a phone number.");

        // 4. Email address leakage — soft flag
        if (EmailRegex.IsMatch(content))
            return ContentPolicyResult.Review("Message may contain an email address.");

        // 5. Excessive URL sharing
        var urlCount = UrlRegex.Matches(content).Count;
        if (urlCount > 2)
            return ContentPolicyResult.Review($"Message contains {urlCount} URLs.");

        // 6. Spam detection via cache — same content within 5 min window
        var spamKey = $"messaging:spam:{senderUserId}:{content.GetHashCode()}";
        var (exists, cached) = await _cache.TryGetAsync<int>(spamKey, ct);
        var count = (exists ? cached : 0) + 1;
        await _cache.SetAsync(count, spamKey,
            new AizenCacheOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5) }, ct);
        if (count >= 3)
            return ContentPolicyResult.Block("Repeated identical message detected.", "SPAM_DETECTED");

        return ContentPolicyResult.Allow();
    }
}
