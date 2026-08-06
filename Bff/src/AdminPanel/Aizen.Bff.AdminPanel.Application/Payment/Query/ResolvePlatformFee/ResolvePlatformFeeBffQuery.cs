using Aizen.Bff.AdminPanel.Application.Payment.Dto;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.Payment.Query.ResolvePlatformFee;


// ─── Resolve (point-in-time preview) ─────────────────────────────────────────
public sealed class ResolvePlatformFeeBffQuery : AizenQuery<ResolvePlatformFeeBffResponse>
{
    public string   CurrencyCode                 { get; init; } = "TRY";
    public string?  CategoryCode                 { get; init; }
    public string?  CustomerType                 { get; init; }
    public decimal  CustomerPayableServiceAmount { get; init; }
}
public sealed class ResolvePlatformFeeBffResponse { public PlatformFeeResolveBffResult? Result { get; init; } }
