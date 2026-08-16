namespace Aizen.Modules.Notification.Abstraction.Response;

/// <summary>
/// M4 — the ONLY thing an anonymous contact submitter sees: whether it was accepted and an opaque ticket reference.
/// Never carries the database id or any internal field (spam score, ip hash, status).
/// </summary>
public sealed class SubmitContactResponse
{
    public bool    Accepted  { get; set; }
    public string? TicketRef { get; set; }
}
