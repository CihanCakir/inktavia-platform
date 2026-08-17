using Aizen.Core.CQRS.Message;
using Aizen.Modules.Payment.Domain.Entities.Plan;

namespace Aizen.Modules.Payment.Application.Commands.UpdateProviderPlan;

public sealed class UpdateProviderPlanCommand : AizenCommand<UpdateProviderPlanResult>
{
    public long    Id               { get; init; }
    public string  Name             { get; init; } = default!;
    public string? Description      { get; init; }
    public decimal MonthlyPriceTRY  { get; init; }
    public decimal? AnnualPriceTRY  { get; init; }
    public int?    TrialDays        { get; init; }
    public string? BadgeLabel       { get; init; }
    public int?    MaxActiveOffers  { get; init; }
    public bool    HasPriorityBoost { get; init; }
    public bool    HasFullAnalytics { get; init; }
    public int     SortOrder        { get; init; }
    public DateTime? ValidFrom      { get; init; }
    public DateTime? ValidTo        { get; init; }
    public List<PlanFeatureItem> FeatureItems { get; init; } = new();
}

public sealed record UpdateProviderPlanResult(long Id, string PlanCode);
