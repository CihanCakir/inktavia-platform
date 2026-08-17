using System.Text.Json;

namespace Aizen.Modules.Identity.Abstraction.Dto.Onboarding;

// ── Request DTOs ─────────────────────────────────────────────────────────────

public sealed class SaveProviderOnboardingStepRequest
{
    public string StepStatus { get; set; } = default!;

    /// <summary>
    /// The step's answers as a raw JSON string.
    ///
    /// This crosses two serializers with opposite ideas of the world: MVC binds request bodies with
    /// <b>Newtonsoft.Json</b> (see <c>AddNewtonsoftJson</c>), while <c>AizenRemoteCall</c>/Refit writes them with
    /// <b>System.Text.Json</b>. Newtonsoft cannot populate an STJ <c>JsonElement</c>, so a <c>JsonElement</c>
    /// property here silently binds to <c>default</c> — the data vanishes with no error, and STJ then throws when
    /// it tries to write that empty element back out. A plain string is the only shape that survives both.
    /// </summary>
    public string StepDataJson { get; set; } = default!;

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

    /// <summary>
    /// The saved draft as a raw JSON string (same serializer trap as <see cref="SaveProviderOnboardingStepRequest.StepDataJson"/>:
    /// Newtonsoft writes the MVC response, Refit reads it with System.Text.Json — only a string survives both).
    /// The BFF parses this before handing it to the browser.
    /// </summary>
    public string? DraftJson { get; set; }

    public string[]? RevisionSteps { get; set; }
    public string? RevisionNote { get; set; }
    public DateTime? LastSavedAtUtc { get; set; }
    public DateTime? SubmittedAtUtc { get; set; }
    public List<ProviderDocumentDto> Documents { get; set; } = new();
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

// ── Document Attach / Remove DTOs ──────────────────────────────────────────

public sealed class AttachProviderDocumentRequest
{
    public Guid FileId { get; set; }
    public string DocumentType { get; set; } = default!;
    public string? Issuer { get; set; }
    public long UserId { get; set; }
}

public sealed class AttachProviderDocumentResponse
{
    public long DocumentId { get; set; }
    public Guid FileId { get; set; }
    public bool Success { get; set; }
    public ProviderDocumentDto? Document { get; set; }
}

public sealed class RemoveProviderDocumentResponse
{
    public bool Success { get; set; }
}

// ── Document DTO ────────────────────────────────────────────────────────────

public sealed class ProviderDocumentDto
{
    public Guid FileId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string? ContentType { get; set; }
    public long SizeInBytes { get; set; }
    public string DocumentType { get; set; } = string.Empty;
    public string? Issuer { get; set; }
    public DateTime UploadedAt { get; set; }
    public string? ReviewStatus { get; set; }
    public string? ResolutionNote { get; set; }
}
