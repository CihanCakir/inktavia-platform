using Aizen.Bff.AdminPanel.Application.AdminPayment.Dto;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.AdminPayment.Command.UpdateProviderPlan;

public sealed class UpdateProviderPlanBffCommand : AizenCommand<PlanMutateBffResult>
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
    public List<PlanFeatureItemBffDto> FeatureItems { get; init; } = new();
}
