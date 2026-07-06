using FluentValidation;

namespace Aizen.Modules.Profile.Application.Queries.Performance.GetProfilePriorityPreview;

/// <summary>
/// Phase 21 — supported contexts (MVP).
/// CargoDryOpportunityRouting and ProviderSearchRanking are deferred.
/// </summary>
public static class SupportedPriorityPreviewContexts
{
    public const string ServiceRequestProviderRecommendation = "ServiceRequestProviderRecommendation";
    public const string AdminAssignmentSuggestion            = "AdminAssignmentSuggestion";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        ServiceRequestProviderRecommendation,
        AdminAssignmentSuggestion,
    };
}

public sealed class GetProfilePriorityPreviewQueryValidator
    : AbstractValidator<GetProfilePriorityPreviewQuery>
{
    public GetProfilePriorityPreviewQueryValidator()
    {
        RuleFor(x => x.CandidateProfileIds)
            .NotNull()
            .NotEmpty().WithMessage("At least one candidate profile ID is required.")
            .Must(ids => ids.Count <= 50).WithMessage("Maximum 50 candidate profile IDs per preview request.");

        RuleForEach(x => x.CandidateProfileIds)
            .GreaterThan(0).WithMessage("All candidate profile IDs must be positive.");

        RuleFor(x => x.Context)
            .NotEmpty()
            .Must(ctx => SupportedPriorityPreviewContexts.All.Contains(ctx))
            .WithMessage($"Unsupported context. Supported (MVP): " +
                         $"{SupportedPriorityPreviewContexts.ServiceRequestProviderRecommendation}, " +
                         $"{SupportedPriorityPreviewContexts.AdminAssignmentSuggestion}.");

        RuleFor(x => x.MaxResults)
            .InclusiveBetween(1, 50).WithMessage("MaxResults must be between 1 and 50.");
    }
}
