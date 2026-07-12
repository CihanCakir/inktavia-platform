using System.Text.Json;
using Aizen.Modules.Identity.Abstraction.Dto.Onboarding;

namespace Aizen.Bff.MarineProvider.Application.Contracts.Onboarding;

public sealed class SaveOnboardingStepRequest
{
    public string StepStatus { get; set; } = default!;
    public JsonElement StepData { get; set; }
    public int SchemaVersion { get; set; } = 1;
}

public sealed class OnboardingResponse
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
    public List<ProviderDocumentDto>? Documents { get; set; }
}

public sealed class SaveOnboardingStepResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
}

public sealed class SubmitOnboardingResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
}
