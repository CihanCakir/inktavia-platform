using System.Collections.Generic;
using Aizen.Core.CQRS.Message;
using Aizen.Modules.Notification.Abstraction.Dto;

namespace Aizen.Modules.Notification.Application.Query.GetNewsletterConfirmedExport;

/// <summary>P1 — the CSV export source: CONFIRMED subscribers only, oldest first.</summary>
public sealed class GetNewsletterConfirmedExportQuery : AizenQuery<List<NewsletterSubscriberListItemDto>>
{
}
