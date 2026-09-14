using Aizen.Core.CQRS.Message;
using Aizen.Modules.ServiceRequest.Abstraction.Response.ServiceRequest;

namespace Aizen.Modules.ServiceRequest.Application.Command.ServiceRequest.CompleteCargoDrySupplyOnActivation;

/// <summary>
/// CargoDry supply flow — invoked (by the mobile BFF, forwarding the owner token) right after a kit is activated.
/// Correlates the activation with the owner's open CARGODRY_SUPPLY request for that vessel+product, and — when one
/// matches — releases the (platform-collected) escrow, completes the SR, and records the sale in CargoDry (attribution
/// + preferred provider). A walk-in activation (no match) or a product/vessel mismatch is a safe no-op.
/// </summary>
public sealed class CompleteCargoDrySupplyOnActivationCommand : AizenCommand<CompleteCargoDrySupplyOnActivationResponse>
{
    public long   KitId       { get; init; }
    public long   VesselId    { get; init; }
    public string ProductCode { get; init; } = default!;
}
