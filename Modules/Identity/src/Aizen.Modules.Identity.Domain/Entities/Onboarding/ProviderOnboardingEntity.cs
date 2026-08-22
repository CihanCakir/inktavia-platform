using System.Text.Json;
using Aizen.Core.Domain;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Core.Common.Abstraction.ViewModel; // FAZ12B #65: AizenErrorCode
using Aizen.Modules.Identity.Domain.Enum;

namespace Aizen.Modules.Identity.Domain.Entities.Onboarding;

public class ProviderOnboardingEntity : AizenEntityWithAudit
{
    private static readonly string[] RequiredSteps =
    {
        nameof(OnboardingStep.BusinessIdentity),
        nameof(OnboardingStep.ServiceCapabilities),
        nameof(OnboardingStep.OperatingRegion),
        nameof(OnboardingStep.ComplianceVerification),
    };

    public long ProfileId { get; set; }
    public long UserId { get; set; }
    public ProviderOnboardingStatus Status { get; private set; } = ProviderOnboardingStatus.NotStarted;
    public int SchemaVersion { get; set; } = 1;
    public string StepStatusesJson { get; set; } = "{}";
    public string DraftJson { get; set; } = "{}";
    public string? RevisionStepsJson { get; set; }
    public string? RevisionNote { get; set; }
    public DateTime? LastSavedAtUtc { get; set; }
    public DateTime? SubmittedAtUtc { get; set; }
    public DateTime? ReviewedAtUtc { get; set; }

    protected ProviderOnboardingEntity() { }

    public static ProviderOnboardingEntity Create(long profileId, long userId)
    {
        return new ProviderOnboardingEntity
        {
            ProfileId = profileId,
            UserId = userId,
            Status = ProviderOnboardingStatus.NotStarted,
            SchemaVersion = 1,
            StepStatusesJson = "{}",
            DraftJson = "{}",
            CreateDate = DateTime.UtcNow,
        };
    }

    public Dictionary<string, string> GetStepStatuses()
    {
        try { return JsonSerializer.Deserialize<Dictionary<string, string>>(StepStatusesJson ?? "{}") ?? new(); }
        catch { return new(); }
    }

    public Dictionary<string, JsonElement> GetDraft()
    {
        try { return JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(DraftJson ?? "{}") ?? new(); }
        catch { return new(); }
    }

    public void SaveStep(string step, string stepStatus, string stepDataJson, DateTime nowUtc)
    {
        if (Status == ProviderOnboardingStatus.Submitted)
            throw new AizenBusinessException((int)AizenErrorCode.ProviderOnboardingLockedAfterSubmit, "Cannot edit onboarding while submitted for review.");

        if (Status == ProviderOnboardingStatus.NotStarted)
            Status = ProviderOnboardingStatus.InProgress;

        // Update step status
        var statuses = GetStepStatuses();
        statuses[step] = stepStatus;
        StepStatusesJson = JsonSerializer.Serialize(statuses);

        // Merge step data into draft (server-side merge — only the specified step's data is replaced)
        var draft = GetDraft();
        draft[step] = JsonSerializer.Deserialize<JsonElement>(stepDataJson);
        DraftJson = JsonSerializer.Serialize(draft);

        LastSavedAtUtc = nowUtc;
        ModifyDate = nowUtc;
    }

    public void Submit(DateTime nowUtc)
    {
        if (Status == ProviderOnboardingStatus.Submitted)
            return; // idempotent

        var statuses = GetStepStatuses();
        foreach (var required in RequiredSteps)
        {
            if (!statuses.TryGetValue(required, out var s) ||
                !string.Equals(s, nameof(OnboardingStepStatus.Completed), StringComparison.OrdinalIgnoreCase))
            {
                throw new AizenBusinessException((int)AizenErrorCode.ProviderOnboardingRequiredStepIncomplete, $"Required step '{required}' is not completed.");
            }
        }

        Status = ProviderOnboardingStatus.Submitted;
        SubmittedAtUtc = nowUtc;
        ModifyDate = nowUtc;
    }

    public void RequestRevision(string[] steps, string note, DateTime nowUtc)
    {
        var statuses = GetStepStatuses();
        foreach (var step in steps)
        {
            statuses[step] = nameof(OnboardingStepStatus.NeedsRevision);
        }
        StepStatusesJson = JsonSerializer.Serialize(statuses);

        RevisionStepsJson = JsonSerializer.Serialize(steps);
        RevisionNote = note;
        Status = ProviderOnboardingStatus.NeedsRevision;
        ReviewedAtUtc = nowUtc;
        ModifyDate = nowUtc;
    }

    public void MarkCompleted(DateTime nowUtc)
    {
        Status = ProviderOnboardingStatus.Completed;
        ReviewedAtUtc = nowUtc;
        ModifyDate = nowUtc;
    }
}
