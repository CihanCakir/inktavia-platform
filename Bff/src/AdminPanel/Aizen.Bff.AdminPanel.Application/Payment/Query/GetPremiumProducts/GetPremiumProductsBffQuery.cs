using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;
using Aizen.Modules.Payment.Abstraction.Dto;

namespace Aizen.Bff.AdminPanel.Application.Payment.Query.GetPremiumProducts;


// ─── List products ───────────────────────────────────────────────────────────
public sealed class GetPremiumProductsBffQuery : AizenQuery<GetPremiumProductsBffResponse> { }
public sealed class GetPremiumProductsBffResponse { public List<PremiumProductAdminDto> Items { get; init; } = new(); }
