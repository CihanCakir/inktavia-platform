using Aizen.Bff.Marine.Web.Application.Common.RemoteClients.Raw;
using Aizen.Core.RemoteCall.Abstraction;

namespace Aizen.Bff.Marine.Web.Application.Common.RemoteClients;

/// <summary>
/// BFF → Payment module PUBLIC plan endpoints for the website pricing page (W4 priority 5, PARTIAL). Both GETs are
/// <c>[AllowAnonymous]</c> on the module ("plans are publicly visible for marketing/pricing pages"), so no service
/// role is required; the outgoing auth handler still attaches the service token, which is harmless. The controllers
/// return the RAW DTO via <c>Ok(dto)</c> — so the typed bodies here are lists of the wire mirrors directly, NOT
/// <c>AizenApiResponse&lt;T&gt;</c>.
///
/// NOTE: only subscription *plans* are public. Commission model, customer platform fee and the VAT flag live on
/// admin-only controllers and are reported BLOCKED in <c>docs/MARINE_WEB_BLOCKED.md</c>.
/// </summary>
public interface IPaymentPlanRemoteCall : IAizenRemoteCall
{
    [AizenRemoteCallGet("/api/v1/payment/provider-plans")]
    Task<List<RawProviderPlanDto>> GetProviderPlans([Refit.Query] bool includeInactive = false);

    [AizenRemoteCallGet("/api/v1/payment/participant-plans")]
    Task<List<RawParticipantPlanDto>> GetParticipantPlans([Refit.Query] bool includeInactive = false);
}
