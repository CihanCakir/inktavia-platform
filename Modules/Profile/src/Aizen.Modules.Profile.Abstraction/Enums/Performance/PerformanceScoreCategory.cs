namespace Aizen.Modules.Profile.Abstraction.Enums.Performance;

/// <summary>
/// Score dimension categories for the performance engine.
/// Weights (Phase 19 spec):
///   ServiceRequest        = 35%
///   CargoDry              = 25%
///   OperationalDiscipline = 15%
///   FinancialReliability  = 15%
///   PlatformCompliance    = 10%
///   RiskPenalty           = deducted (not a weighted category)
/// </summary>
public enum PerformanceScoreCategory
{
    ServiceRequest        = 1,
    CargoDry              = 2,
    OperationalDiscipline = 3,
    FinancialReliability  = 4,
    PlatformCompliance    = 5,

    /// <summary>Penalty points deducted from the final weighted sum.</summary>
    RiskPenalty           = 6,
}
