using Aizen.Bff.AdminPanel.Application.Payment.Dto;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.Payment.Command.CreateParticipantPlan;

public sealed class CreateParticipantPlanBffCommand : AizenCommand<PlanMutateBffResult>
{
    public string  PlanCode               { get; init; } = default!;
    public string  Name                   { get; init; } = default!;
    public string? Description            { get; init; }
    public decimal MonthlyPriceTRY        { get; init; }
    public decimal? AnnualPriceTRY        { get; init; }
    public int?    TrialDays              { get; init; }
    public string? BadgeLabel             { get; init; }
    public decimal ServiceDiscountRate    { get; init; }
    public decimal CargoDryDiscountRate   { get; init; }
    public decimal InkCoinEarnMultiplier  { get; init; }
    public int     SortOrder              { get; init; }
    public DateTime? ValidFrom            { get; init; }
    public DateTime? ValidTo              { get; init; }
    public List<PlanFeatureItemBffDto> FeatureItems { get; init; } = new();
}
