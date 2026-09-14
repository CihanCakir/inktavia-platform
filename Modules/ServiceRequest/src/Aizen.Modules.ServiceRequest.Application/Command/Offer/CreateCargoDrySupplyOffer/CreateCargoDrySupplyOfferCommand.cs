using Aizen.Core.CQRS.Message;
using Aizen.Modules.ServiceRequest.Abstraction.Response.Offer;

namespace Aizen.Modules.ServiceRequest.Application.Command.Offer.CreateCargoDrySupplyOffer;

/// <summary>
/// CargoDry supply flow — a program provider "accepts" a CARGODRY_SUPPLY request. There is NO bidding: the server pins
/// a single offer at the product's fixed retail price (no editable amount). Gated server-side — only a provider with an
/// ACTIVE ConsignmentAgreement for the product may accept. Provider identity is taken from the token, never the body.
/// </summary>
public sealed class CreateCargoDrySupplyOfferCommand : AizenCommand<CreateCargoDrySupplyOfferResponse>
{
    public long ServiceRequestId { get; init; }
}
