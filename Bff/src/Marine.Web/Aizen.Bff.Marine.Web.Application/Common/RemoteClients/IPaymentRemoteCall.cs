using Aizen.Bff.Marine.Web.Application.Common.RemoteClients.Raw;
using Aizen.Core.RemoteCall.Abstraction;
using Aizen.Modules.Payment.Abstraction.Dto;

namespace Aizen.Bff.Marine.Web.Application.Common.RemoteClients;

/// <summary>
/// BFF → Payment module PUBLIC endpoints for the website pricing page (W4 plans + M1 pricing terms). All are
/// <c>[AllowAnonymous]</c> on the module ("plans are publicly visible for marketing/pricing pages"; the pricing-terms
/// read mirrors them), so no service role is required; the outgoing auth handler still attaches the service token,
/// which is harmless. The controllers return the RAW DTO via <c>Ok(dto)</c> — so the typed bodies here are the wire
/// shapes directly, NOT <c>AizenApiResponse&lt;T&gt;</c>.
///
/// Plans bind BFF-local wire mirrors (their module DTOs are not all in <c>Payment.Abstraction</c>). The pricing-terms
/// read binds the real <see cref="PublicPricingTermsDto"/> from <c>Payment.Abstraction</c> (already field-stripped at
/// the module — only the Global published headline figures; no economics internals).
/// </summary>
public interface IPaymentRemoteCall : IAizenRemoteCall
{
    [AizenRemoteCallGet("/api/v1/payment/provider-plans")]
    Task<List<RawProviderPlanDto>> GetProviderPlans([Refit.Query] bool includeInactive = false);

    [AizenRemoteCallGet("/api/v1/payment/participant-plans")]
    Task<List<RawParticipantPlanDto>> GetParticipantPlans([Refit.Query] bool includeInactive = false);

    [AizenRemoteCallGet("/api/v1/payment/public/pricing-terms")]
    Task<PublicPricingTermsDto> GetPublicPricingTerms();
}
