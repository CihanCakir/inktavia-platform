namespace Aizen.Modules.Identity.Abstraction.Dto.Common;

public class VerificationDocumentDto
{
    public long Id { get; set; }
    public long FileId { get; set; }
    public string Name { get; set; } = null!;
    public string DocumentType { get; set; } = null!;
    public string? Format { get; set; }
    public string? FileSizeDisplay { get; set; }
    public string? Issuer { get; set; }
    public string? MatchScore { get; set; }
    public string? UploadedAt { get; set; }
}
