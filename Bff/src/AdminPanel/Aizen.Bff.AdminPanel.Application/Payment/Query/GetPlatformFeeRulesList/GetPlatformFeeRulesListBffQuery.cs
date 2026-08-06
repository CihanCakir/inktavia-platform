using Aizen.Bff.AdminPanel.Application.Payment.Dto;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.Payment.Query.GetPlatformFeeRulesList;


// ─── List (paged + filters) ──────────────────────────────────────────────────
public sealed class GetPlatformFeeRulesListBffQuery : AizenQuery<GetPlatformFeeRulesListBffResponse>
{
    public string?  Model        { get; init; }
    public string?  Status       { get; init; }
    public string?  CurrencyCode { get; init; }
    public string?  CategoryCode { get; init; }
    public string?  CustomerType { get; init; }
    public int      Page         { get; init; } = 1;
    public int      PageSize     { get; init; } = 20;
}
public sealed class GetPlatformFeeRulesListBffResponse { public PlatformFeeRulesListBffResult Result { get; init; } = default!; }
