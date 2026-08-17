using Aizen.Bff.AdminPanel.Application.Users.Dto;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.Users.Query;

[DocumentationInfo("Get admin user activity BFF query", "Returns the paginated, filterable activity timeline for a specific user.")]
public sealed class GetUserActivityBffQuery : AizenQuery<AdminUserActivityBffResponse>
{
    public long ProfileId { get; }
    public string? Category { get; }   // vessel | service | identity | system | null = all
    public DateOnly? DateFrom { get; }
    public DateOnly? DateTo { get; }
    public int Page { get; }
    public int PageSize { get; }

    public GetUserActivityBffQuery(
        long profileId,
        string? category = null,
        DateOnly? dateFrom = null,
        DateOnly? dateTo = null,
        int page = 1,
        int pageSize = 20)
    {
        ProfileId = profileId;
        Category = category?.ToLowerInvariant();
        DateFrom = dateFrom;
        DateTo = dateTo;
        Page = Math.Max(1, page);
        PageSize = Math.Clamp(pageSize, 1, 100);
    }
}
