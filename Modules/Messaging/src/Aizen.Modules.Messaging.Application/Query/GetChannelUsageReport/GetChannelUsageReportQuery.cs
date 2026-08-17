using Aizen.Core.CQRS.Message;

namespace Aizen.Modules.Messaging.Application.Query.GetChannelUsageReport;

[DocumentationInfo("Get channel usage report query",
    "Message volume by context type, daily trend, and peak hour breakdown.")]
public sealed class GetChannelUsageReportQuery : AizenQuery<GetChannelUsageReportResponse>
{
    public DateTimeOffset From { get; init; } = DateTimeOffset.UtcNow.AddDays(-30);
    public DateTimeOffset To   { get; init; } = DateTimeOffset.UtcNow;
}

public sealed record GetChannelUsageReportResponse(
    List<ChannelVolumeDto> ByChannel,
    List<DailyMessageVolumeDto> DailyTrend,
    List<PeakHourDto> PeakHours,
    DateTimeOffset ReportGeneratedAt
);

public sealed record ChannelVolumeDto(
    string ContextType,
    int ConversationCount,
    int MessageCount,
    double AvgMessagesPerConversation
);

public sealed record DailyMessageVolumeDto(
    DateOnly Date,
    int MessageCount,
    int NewConversations
);

public sealed record PeakHourDto(int Hour, int MessageCount);
