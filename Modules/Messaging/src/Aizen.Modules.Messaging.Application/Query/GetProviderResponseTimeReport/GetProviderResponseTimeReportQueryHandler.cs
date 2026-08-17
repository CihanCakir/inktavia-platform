using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Messaging.Abstraction.Enum;
using Aizen.Modules.Messaging.Repository.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Aizen.Modules.Messaging.Application.Query.GetProviderResponseTimeReport;

[DocumentationInfo("Get provider response time report query handler",
    "Computes average first-response time per provider using ServiceRequest conversation messages.")]
public sealed class GetProviderResponseTimeReportQueryHandler
    : AizenQueryHandler<GetProviderResponseTimeReportQuery, GetProviderResponseTimeReportResponse>
{
    private readonly MessagingDbContext _db;

    public GetProviderResponseTimeReportQueryHandler(MessagingDbContext db) => _db = db;

    public override async Task<GetProviderResponseTimeReportResponse> Handle(
        GetProviderResponseTimeReportQuery query, CancellationToken ct)
    {
        var conversationsQuery = _db.Conversations
            .AsNoTracking()
            .Where(c => c.ContextType == MessagingContextType.ServiceRequest && !c.IsDeleted);

        if (query.From.HasValue) conversationsQuery = conversationsQuery.Where(c => c.CreateDate >= query.From.Value.DateTime);
        if (query.To.HasValue)   conversationsQuery = conversationsQuery.Where(c => c.CreateDate <= query.To.Value.DateTime);

        var conversations = await conversationsQuery
            .Select(c => new
            {
                c.Id,
                FirstOwnerMessageAt = _db.ConversationMessages
                    .Where(m => m.ConversationId == c.Id
                             && m.SenderRole == MessagingParticipantRole.Owner
                             && !m.IsDeleted)
                    .OrderBy(m => m.SentAt)
                    .Select(m => (DateTimeOffset?)m.SentAt)
                    .FirstOrDefault(),
                FirstProviderMessageAt = _db.ConversationMessages
                    .Where(m => m.ConversationId == c.Id
                             && m.SenderRole == MessagingParticipantRole.Provider
                             && !m.IsDeleted)
                    .OrderBy(m => m.SentAt)
                    .Select(m => (DateTimeOffset?)m.SentAt)
                    .FirstOrDefault(),
                ProviderUserId = _db.ConversationParticipants
                    .Where(p => p.ConversationId == c.Id
                             && p.Role == MessagingParticipantRole.Provider)
                    .Select(p => (long?)p.UserId)
                    .FirstOrDefault(),
                ProviderName = _db.ConversationParticipants
                    .Where(p => p.ConversationId == c.Id
                             && p.Role == MessagingParticipantRole.Provider)
                    .Select(p => p.DisplayName)
                    .FirstOrDefault()
            })
            .Where(x => x.ProviderUserId != null)
            .ToListAsync(ct);

        var grouped = conversations
            .GroupBy(x => new { x.ProviderUserId, x.ProviderName })
            .Select(g =>
            {
                var answered = g.Where(x =>
                    x.FirstOwnerMessageAt.HasValue &&
                    x.FirstProviderMessageAt.HasValue &&
                    x.FirstProviderMessageAt > x.FirstOwnerMessageAt).ToList();

                var avgMinutes = answered.Any()
                    ? answered.Average(x =>
                        (x.FirstProviderMessageAt!.Value - x.FirstOwnerMessageAt!.Value).TotalMinutes)
                    : 0;

                return new ProviderResponseTimeDto(
                    g.Key.ProviderUserId!.Value,
                    g.Key.ProviderName ?? "Unknown",
                    Math.Round(avgMinutes, 1),
                    g.Count(),
                    g.Count() - answered.Count);
            })
            .OrderBy(x => x.AvgFirstResponseMinutes)
            .Take(query.Take)
            .ToList();

        return new GetProviderResponseTimeReportResponse(grouped, DateTimeOffset.UtcNow);
    }
}
