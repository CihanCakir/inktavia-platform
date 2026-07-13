using Aizen.Modules.Identity.Abstraction.Dto.Onboarding;
using Newtonsoft.Json.Linq;

namespace Aizen.Bff.MarineProvider.Application.Contracts.Onboarding;

public sealed class SaveOnboardingStepRequest
{
    public string StepStatus { get; set; } = default!;

    /// <summary>
    /// The step's answers, as sent by the SPA.
    ///
    /// MVC binds request bodies with <b>Newtonsoft.Json</b> (<c>AddNewtonsoftJson</c>). A
    /// <c>System.Text.Json.JsonElement</c> here binds to <c>default</c> — Newtonsoft cannot populate it — so the
    /// answers silently disappear and the outbound Refit call then throws trying to serialise an empty element.
    /// <see cref="JToken"/> is Newtonsoft's own type and binds correctly.
    /// </summary>
    public JToken? StepData { get; set; }

    public int SchemaVersion { get; set; } = 1;
}

public sealed class OnboardingResponse
{
    public long ProfileId { get; set; }
    public string Status { get; set; } = string.Empty;
    public int SchemaVersion { get; set; }
    public Dictionary<string, string> StepStatuses { get; set; } = new();

    /// <summary>The draft handed to the SPA as a real JSON object. Newtonsoft serialises <see cref="JToken"/> natively.</summary>
    public JToken? Draft { get; set; }

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
