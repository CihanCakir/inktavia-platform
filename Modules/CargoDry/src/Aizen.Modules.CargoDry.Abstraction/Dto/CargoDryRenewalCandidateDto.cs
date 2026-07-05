using Aizen.Modules.CargoDry.Abstraction.Enum;

namespace Aizen.Modules.CargoDry.Abstraction.Dto;

public sealed class CargoDryRenewalCandidateDto
{
    public long               KitId                            { get; init; }
    public string             KitCode                          { get; init; } = default!;
    public string             SerialNumber                     { get; init; } = default!;
    public string             ProductCode                      { get; init; } = default!;
    public string?            ProductName                      { get; init; }
    public string             BatchCode                        { get; init; } = default!;
    public CargoDryKitStatus  Status                           { get; init; }
    public long?              OwnerUserId                      { get; init; }
    public string?            OwnerDisplayName                 { get; init; }
    public long?              VesselId                         { get; init; }
    public string?            VesselName                       { get; init; }
    public long?              ProviderProfileId                { get; init; }
    public DateTimeOffset?    ExpiresAtUtc                     { get; init; }
    public int                DaysUntilExpiry                  { get; init; }
    public int                RecommendedRenewalMonths         { get; init; }
    public decimal?           RenewalPrice                     { get; init; }
    public string?            CurrencyCode                     { get; init; }
    public bool               CanPrepareRenewal                { get; init; }
    public List<string>       BlockingReasons                  { get; init; } = new();
    public List<string>       Warnings                         { get; init; } = new();
    public DateTimeOffset?    LastRenewalAtUtc                 { get; init; }
    public DateTimeOffset?    LastNotificationPreparedAtUtc    { get; init; }
    public DateTimeOffset?    LastNotificationDispatchedAtUtc  { get; init; }
    public string?            LastNotificationStatus           { get; init; }
    public long?              OpenRenewalPreparationId         { get; init; }
}
