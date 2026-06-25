using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Messaging.Repository.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Aizen.Modules.Messaging.Application.Query.GetChannelUsageReport;

[DocumentationInfo("Get channel usage report query handler",
    "Aggregates message volume by context type, daily trend, and peak hours.")]
public sealed class GetChannelUsageReportQueryHandler
    : AizenQueryHandler<GetChannelUsageReportQuery, GetChannelUsageReportResponse>
{
    private readonly MessagingDbContext _db;

    public GetChannelUsageReportQueryHandler(MessagingDbContext db) => _db = db;

    public override async Task<GetChannelUsageReportResponse> Handle(
        GetChannelUsageReportQuery query, CancellationToken ct)
    {
        var from = query.From;
        var to   = query.To;

        // Channel volume
        var byChannel = await _db.Conversations
            .AsNoTracking()
            .Where(c => c.CreateDate >= from.DateTime && c.CreateDate <= to.DateTime && !c.IsDeleted)
            .GroupBy(c => c.ContextType)
            .Select(g => new
            {
                ContextType       = g.Key.ToString(),
                ConversationCount = g.Count(),
                MessageCount      = _db.ConversationMessages
                    .Count(m => g.Select(c => c.Id).Contains(m.ConversationId) && !m.IsDeleted)
            })
            .ToListAsync(ct);

        var channelDtos = byChannel.Select(x => new ChannelVolumeDto(
            x.ContextType,
            x.ConversationCount,
            x.MessageCount,
            x.ConversationCount > 0
                ? Math.Round((double)x.MessageCount / x.ConversationCount, 1)
                : 0
        )).ToList();

        // Daily trend
        var dailyTrend = await _db.ConversationMessages
            .AsNoTracking()
            .Where(m => m.SentAt >= from && m.SentAt <= to && !m.IsDeleted)
            .GroupBy(m => m.SentAt.Date)
            .OrderBy(g => g.Key)
            .Select(g => new DailyMessageVolumeDto(
                DateOnly.FromDateTime(g.Key),
                g.Count(),
                _db.Conversations.Count(c => c.CreateDate!.Value.Date == g.Key && !c.IsDeleted)
            ))
            .ToListAsync(ct);

        // Peak hours (UTC)
        var peakHours = await _db.ConversationMessages
            .AsNoTracking()
            .Where(m => m.SentAt >= from && m.SentAt <= to && !m.IsDeleted)
            .GroupBy(m => m.SentAt.Hour)
            .OrderBy(g => g.Key)
            .Select(g => new PeakHourDto(g.Key, g.Count()))
            .ToListAsync(ct);

        return new GetChannelUsageReportResponse(
            channelDtos, dailyTrend, peakHours, DateTimeOffset.UtcNow);
    }
}
