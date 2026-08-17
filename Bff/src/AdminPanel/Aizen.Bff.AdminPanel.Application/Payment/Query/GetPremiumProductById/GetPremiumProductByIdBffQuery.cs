using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;
using Aizen.Modules.Payment.Abstraction.Dto;

namespace Aizen.Bff.AdminPanel.Application.Payment.Query.GetPremiumProductById;


// ─── Get product by id ───────────────────────────────────────────────────────
public sealed class GetPremiumProductByIdBffQuery : AizenQuery<GetPremiumProductByIdBffResponse>
{
    public long Id { get; init; }
}
public sealed class GetPremiumProductByIdBffResponse { public PremiumProductAdminDto? Result { get; init; } }
