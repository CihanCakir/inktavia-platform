namespace Aizen.Modules.Notification.Application.Command.SubmitContactMessage;

/// <summary>
/// M4 — pure server-side spam heuristics (0..100). The submission-rate penalty is applied by the handler (it needs a
/// repository count); everything here is a pure function of the untrusted payload. A non-empty honeypot short-circuits
/// to the max score — a real user never fills a hidden field.
/// </summary>
public static class ContactSpamScorer
{
    public const int MaxScore = 100;

    public static int Score(string? subject, string? message, string? honeypot, ContactIntakeOptions options)
    {
        if (!string.IsNullOrWhiteSpace(honeypot))
            return MaxScore;   // a bot filled the hidden field

        var score = 0;
        var text = $"{subject}\n{message}";

        var links = CountLinks(text);
        if (links > options.MaxLinks) score += 40;
        else if (links > 0) score += links * 10;

        var body = (message ?? string.Empty).Trim();
        if (body.Length < 15) score += 25;     // too short to be a real enquiry
        if (body.Length > 4000) score += 15;   // wall of text

        score += AllCapsPenalty(body);

        return Math.Min(score, MaxScore);
    }

    private static int CountLinks(string text)
    {
        var lower = text.ToLowerInvariant();
        var count = 0;
        foreach (var marker in new[] { "http://", "https://", "www." })
        {
            var idx = 0;
            while ((idx = lower.IndexOf(marker, idx, StringComparison.Ordinal)) >= 0)
            {
                count++;
                idx += marker.Length;
            }
        }
        return count;
    }

    private static int AllCapsPenalty(string body)
    {
        var letters = 0;
        var upper = 0;
        foreach (var ch in body)
        {
            if (!char.IsLetter(ch)) continue;
            letters++;
            if (char.IsUpper(ch)) upper++;
        }
        return letters >= 20 && (double)upper / letters > 0.7 ? 20 : 0;
    }
}
