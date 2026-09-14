using Aizen.Bff.Marine.Participant.Mobile.Application.Contracts.CargoDry;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.Marine.Participant.Mobile.Application.CargoDry;

/// <summary>GET /api/v1/mobile/cargodry/products — owner-safe active product catalog with presigned media URLs.</summary>
public sealed class GetMobileCargoDryProductsQuery : AizenQuery<List<MobileCargoDryProductDto>>
{
}
