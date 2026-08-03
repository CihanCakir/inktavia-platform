namespace Aizen.Bff.AdminPanel.Application.Common.Dto;

// ─── Messaging reporting contracts ──────────────────────────────────────────
// The Messaging module's report response records live in its .Application layer
// (Aizen.Modules.Messaging.Application.Query.*) which the BFF does not — and should not —
// reference. These BFF-local records mirror the module DTOs; Refit/System.Text.Json
// deserialize the wrapped module envelope by JSON property name, so the shapes line up.
// Wave 3 replaces the previous Task<object> remote calls + anonymous merge with these.

// Mirror of GetProviderResponseTimeReportResponse (the wrapped module body).
public sealed record ProviderResponseTimeReportBody(
    List<ProviderResponseTimeItem> Items,
    DateTimeOffset ReportGeneratedAt);

public sealed record ProviderResponseTimeItem(
    long ProviderUserId,
    string ProviderName,
    double AvgFirstResponseMinutes,
    int TotalConversations,
    int UnansweredConversations);

// Mirror of GetChannelUsageReportResponse (the wrapped module body).
public sealed record ChannelUsageReportBody(
    List<ChannelVolumeItem> ByChannel,
    List<DailyMessageVolumeItem> DailyTrend,
    List<PeakHourItem> PeakHours,
    DateTimeOffset ReportGeneratedAt);

public sealed record ChannelVolumeItem(
    string ContextType,
    int ConversationCount,
    int MessageCount,
    double AvgMessagesPerConversation);

public sealed record DailyMessageVolumeItem(
    DateOnly Date,
    int MessageCount,
    int NewConversations);

public sealed record PeakHourItem(int Hour, int MessageCount);

// ─── Merged BFF response ────────────────────────────────────────────────────
// GetReports fans the two module calls out, unwraps each envelope's .Body, and returns
// this single typed record (no more anonymous object). FE consumes it as
// { providerResponseTime[], channelUsage: { byChannel[], dailyTrend[], peakHours[] } }.
public sealed record MessagingReportsBffResponse(
    List<ProviderResponseTimeItem> ProviderResponseTime,
    ChannelUsageBff ChannelUsage,
    DateTimeOffset GeneratedAt);

public sealed record ChannelUsageBff(
    List<ChannelVolumeItem> ByChannel,
    List<DailyMessageVolumeItem> DailyTrend,
    List<PeakHourItem> PeakHours);
