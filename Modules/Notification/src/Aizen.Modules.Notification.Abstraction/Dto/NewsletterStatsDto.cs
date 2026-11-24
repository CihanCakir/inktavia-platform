namespace Aizen.Modules.Notification.Abstraction.Dto;

/// <summary>P1 — newsletter dashboard counters: one count per status plus new sign-ups in the last 30 days.</summary>
public sealed class NewsletterStatsDto
{
    public int Total          { get; set; }
    public int Pending        { get; set; }
    public int Confirmed      { get; set; }
    public int Unsubscribed   { get; set; }
    public int Bounced        { get; set; }
    public int Suppressed     { get; set; }
    public int NewInLast30Days { get; set; }
}
