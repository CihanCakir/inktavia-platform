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
            // CreateDate is a timestamptz column; DateTimeOffset.DateTime yields Kind=Unspecified which Npgsql rejects.
            // Use UtcDateTime (Kind=Utc). The SentAt filters below compare DateTimeOffset directly, so they're unaffected.
            .Where(c => c.CreateDate >= from.UtcDateTime && c.CreateDate <= to.UtcDateTime && !c.IsDeleted)
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

        // Daily trend + peak hours — aggregate in memory (like the provider report does). Npgsql cannot
        // translate DateOnly/hour truncation (.Date/.Hour) over the timestamptz columns, so we pull the raw
        // timestamps for the range and bucket them client-side by their UTC day / UTC hour.
        var messageSentAts = await _db.ConversationMessages
            .AsNoTracking()
            .Where(m => m.SentAt >= from && m.SentAt <= to && !m.IsDeleted)
            .Select(m => m.SentAt)
            .ToListAsync(ct);

        var newConversationDates = await _db.Conversations
            .AsNoTracking()
            .Where(c => c.CreateDate != null
                     && c.CreateDate >= from.UtcDateTime
                     && c.CreateDate <= to.UtcDateTime
                     && !c.IsDeleted)
            .Select(c => c.CreateDate!.Value)
            .ToListAsync(ct);

        var newConversationsByDay = newConversationDates
            .GroupBy(d => DateOnly.FromDateTime(d))
            .ToDictionary(g => g.Key, g => g.Count());

        var dailyTrend = messageSentAts
            .GroupBy(ts => DateOnly.FromDateTime(ts.UtcDateTime))
            .OrderBy(g => g.Key)
            .Select(g => new DailyMessageVolumeDto(
                g.Key,
                g.Count(),
                newConversationsByDay.TryGetValue(g.Key, out var n) ? n : 0))
            .ToList();

        var peakHours = messageSentAts
            .GroupBy(ts => ts.UtcDateTime.Hour)
            .OrderBy(g => g.Key)
            .Select(g => new PeakHourDto(g.Key, g.Count()))
            .ToList();

        return new GetChannelUsageReportResponse(
            channelDtos, dailyTrend, peakHours, DateTimeOffset.UtcNow);
    }
}
