using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.Notifications.Query;

/// <summary>P1 — proxies the module's CONFIRMED-only CSV export (raw text/csv, re-emitted by the controller as a file).</summary>
public sealed class ExportNewsletterBffQuery : AizenQuery<string>
{
}
