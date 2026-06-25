using Aizen.Core.CQRS.Message;

namespace Aizen.Modules.Messaging.Application.Query.GetProviderResponseTimeReport;

[DocumentationInfo("Get provider response time report query",
    "Aggregates first-response time per provider for ServiceRequest conversations.")]
public sealed class GetProviderResponseTimeReportQuery
    : AizenQuery<GetProviderResponseTimeReportResponse>
{
    public DateTimeOffset? From { get; init; }
    public DateTimeOffset? To   { get; init; }
    public int Take             { get; init; } = 20;
}

public sealed record GetProviderResponseTimeReportResponse(
    List<ProviderResponseTimeDto> Items,
    DateTimeOffset ReportGeneratedAt
);

public sealed record ProviderResponseTimeDto(
    long ProviderUserId,
    string ProviderName,
    double AvgFirstResponseMinutes,
    int TotalConversations,
    int UnansweredConversations
);
