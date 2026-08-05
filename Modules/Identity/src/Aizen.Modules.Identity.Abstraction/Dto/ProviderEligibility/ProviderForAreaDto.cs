namespace Aizen.Modules.Identity.Abstraction.Dto.ProviderEligibility;

/// <summary>
/// I2 read-model row — an eligible (approved + active) provider for a given area. Shared by the Identity endpoint and
/// the Notification remote call (N-C region fan-out). ProfileId is the ProviderProfileId (notification recipient id).
/// </summary>
public sealed class ProviderForAreaDto
{
    public long ProfileId { get; set; }
    public long UserId    { get; set; }
}
