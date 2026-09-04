using System.Text.Json;
using FirebaseAdmin.Messaging;

namespace Aizen.Modules.Notification.Application.Services.Firebase;

/// <summary>
/// BE-MO9a — maps our push payload (title / body / data-json) → a FirebaseAdmin cross-platform message. Adapts
/// the legacy <c>CreateFirebaseMessage</c> to our shape: our senders carry only final title/body strings + a
/// <c>Data</c> JSON blob (the deep-link ref) — localization/interpolation already happened upstream. A single
/// <see cref="Message"/> carries the top-level notification + an APNs <c>Aps</c> (alert, sound, mutable-content,
/// thread-id) + an Android notification + the <c>Data</c> dict; FCM applies the override for the token's platform.
/// Pure + FirebaseAdmin-model-only (no app/credential state) so it is unit-testable with no Firebase creds.
/// </summary>
public static class FcmMessageMapper
{
    /// <summary>FCM multicast hard limit — one <see cref="MulticastMessage"/> may target at most this many tokens.</summary>
    public const int MaxMulticastBatch = 500;

    /// <summary>Parses the data-json blob (e.g. <c>{notificationId,referenceType,referenceId}</c>) into a flat
    /// string→string dictionary (FCM data must be string-valued). Null/blank/invalid → empty.</summary>
    public static Dictionary<string, string> ParseData(string? dataJson)
    {
        var data = new Dictionary<string, string>();
        if (string.IsNullOrWhiteSpace(dataJson)) return data;
        try
        {
            using var doc = JsonDocument.Parse(dataJson);
            if (doc.RootElement.ValueKind != JsonValueKind.Object) return data;
            foreach (var p in doc.RootElement.EnumerateObject())
            {
                var v = p.Value.ValueKind switch
                {
                    JsonValueKind.String => p.Value.GetString() ?? string.Empty,
                    JsonValueKind.Null   => string.Empty,
                    _                    => p.Value.GetRawText(),
                };
                data[p.Name] = v;
            }
        }
        catch (JsonException)
        {
            // A malformed data blob must never break the push — send it with no data.
        }
        return data;
    }

    /// <summary>Builds a single-token cross-platform message (APNs + Android + Data).</summary>
    public static Message CreateMessage(string deviceToken, string title, string body, string? dataJson)
    {
        var data = ParseData(dataJson);
        return new Message
        {
            Token        = deviceToken,
            Notification = new FirebaseAdmin.Messaging.Notification { Title = title, Body = body },
            Data         = data,
            Apns         = BuildApns(title, body, data),
            Android      = BuildAndroid(title, body, data),
        };
    }

    /// <summary>Builds a multicast message for up to <see cref="MaxMulticastBatch"/> tokens (chunk larger sets first).</summary>
    public static MulticastMessage CreateMulticast(IReadOnlyList<string> deviceTokens, string title, string body, string? dataJson)
    {
        var data = ParseData(dataJson);
        return new MulticastMessage
        {
            Tokens       = deviceTokens.ToList(),
            Notification = new FirebaseAdmin.Messaging.Notification { Title = title, Body = body },
            Data         = data,
            Apns         = BuildApns(title, body, data),
            Android      = BuildAndroid(title, body, data),
        };
    }

    /// <summary>Splits a token list into ≤<paramref name="size"/> chunks (FCM's 500-token multicast cap).</summary>
    public static IReadOnlyList<IReadOnlyList<string>> ChunkTokens(IReadOnlyList<string> tokens, int size = MaxMulticastBatch)
    {
        if (size < 1) size = MaxMulticastBatch;
        var chunks = new List<IReadOnlyList<string>>();
        for (var i = 0; i < tokens.Count; i += size)
            chunks.Add(tokens.Skip(i).Take(size).ToList());
        return chunks;
    }

    private static ApnsConfig BuildApns(string title, string body, IReadOnlyDictionary<string, string> data)
    {
        var threadId = data.TryGetValue("referenceId", out var rid) ? rid : null;
        return new ApnsConfig
        {
            Aps = new Aps
            {
                Alert            = new ApsAlert { Title = title, Body = body },
                Sound            = "default",
                MutableContent   = true,
                ContentAvailable = true,
                ThreadId         = threadId,
            },
            Headers = new Dictionary<string, string> { ["apns-push-type"] = "alert" },
        };
    }

    private static AndroidConfig BuildAndroid(string title, string body, IReadOnlyDictionary<string, string> data)
        => new()
        {
            CollapseKey  = data.TryGetValue("referenceType", out var rt) ? rt : null,
            Notification = new AndroidNotification { Title = title, Body = body, DefaultSound = true },
        };
}
