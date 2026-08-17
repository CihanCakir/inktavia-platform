namespace Aizen.Modules.Payment.Domain.Entities.Plan;

/// <summary>
/// A single feature bullet shown on a pricing/plan card.
/// Stored as JSONB inside the plan entity.
/// </summary>
public sealed record PlanFeatureItem(string Text, bool IsHighlighted = false);
