namespace Aizen.Modules.CargoDry.Domain.Entities;

public sealed class CargoDryProviderMilestoneAwardEntity
{
    public long   Id                { get; private set; }
    public long   ProviderProfileId { get; private set; }
    public string MilestoneType     { get; private set; } = default!;
    public string PeriodKey         { get; private set; } = default!;
    public string? DisplayValue     { get; private set; }
    public DateTime AwardedAtUtc    { get; private set; }
    public bool   NotificationPublished { get; private set; }

    private CargoDryProviderMilestoneAwardEntity() { }

    public static CargoDryProviderMilestoneAwardEntity Create(
        long providerProfileId, string milestoneType, string periodKey, string? displayValue, DateTime nowUtc)
        => new()
        {
            ProviderProfileId = providerProfileId,
            MilestoneType     = milestoneType,
            PeriodKey         = periodKey,
            DisplayValue      = displayValue,
            AwardedAtUtc      = nowUtc,
            NotificationPublished = false,
        };

    public void MarkPublished() => NotificationPublished = true;
}
