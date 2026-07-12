using System.Text.Json;

namespace Aizen.Modules.Identity.Abstraction.Dto.Onboarding;

// ── Request DTOs ─────────────────────────────────────────────────────────────

public sealed class SaveProviderOnboardingStepRequest
{
    public string StepStatus { get; set; } = default!;
    public JsonElement StepData { get; set; }
    public int SchemaVersion { get; set; } = 1;
}

public sealed class SubmitProviderOnboardingRequest { }

public sealed class RequestProviderOnboardingRevisionRequest
{
    public string[] Steps { get; set; } = Array.Empty<string>();
    public string Note { get; set; } = default!;
}

// ── Response DTOs ────────────────────────────────────────────────────────────

public sealed class ProviderOnboardingResponse
{
    public long ProfileId { get; set; }
    public string Status { get; set; } = string.Empty;
    public int SchemaVersion { get; set; }
    public Dictionary<string, string> StepStatuses { get; set; } = new();
    public JsonElement? Draft { get; set; }
    public string[]? RevisionSteps { get; set; }
    public string? RevisionNote { get; set; }
    public DateTime? LastSavedAtUtc { get; set; }
    public DateTime? SubmittedAtUtc { get; set; }
}

public sealed class SaveProviderOnboardingStepResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
}

public sealed class SubmitProviderOnboardingResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
}

public sealed class RequestProviderOnboardingRevisionResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
}
