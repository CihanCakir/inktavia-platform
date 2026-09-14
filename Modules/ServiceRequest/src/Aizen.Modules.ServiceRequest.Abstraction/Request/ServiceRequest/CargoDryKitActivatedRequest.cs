namespace Aizen.Modules.ServiceRequest.Abstraction.Request.ServiceRequest;

/// <summary>
/// CargoDry supply flow: posted by the mobile BFF right after a kit is activated, so the ServiceRequest module can
/// correlate the activation with the owner's open CARGODRY_SUPPLY request (owner id comes from the forwarded token).
/// </summary>
public sealed class CargoDryKitActivatedRequest
{
    public long   KitId       { get; set; }
    public long   VesselId    { get; set; }
    public string ProductCode { get; set; } = default!;
}
