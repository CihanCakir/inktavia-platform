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
