namespace Aizen.Modules.Payment.Abstraction.Enum;

/// <summary>
/// Label/reporting tag for a provider plan price version (BE-P4, §13.2). This is display/reporting ONLY —
/// price resolution is strictly date-driven (point-in-time on [EffectiveFrom, EffectiveTo)), never by this type.
/// Launch = the global go-live campaign window; List = the standing price afterwards.
/// </summary>
public enum ProviderPlanPriceType
{
    Launch = 1,
    List   = 2,
}
