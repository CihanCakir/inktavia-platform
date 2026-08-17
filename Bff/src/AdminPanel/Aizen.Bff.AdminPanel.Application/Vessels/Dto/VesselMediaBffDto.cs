using Aizen.Bff.AdminPanel.Application.Common;

namespace Aizen.Bff.AdminPanel.Application.Vessels.Dto;

[DocumentationInfo("Vessel media BFF DTO", "Vessel media item with enriched fields for the Media tab.")]
public sealed class VesselMediaBffDto
{
    public long Id { get; set; }
    public string? MediaType { get; set; }
    public string? Title { get; set; }
    public string? Description { get; set; }
    public string? OriginalFileName { get; set; }
    public string? ContentType { get; set; }
    public long? FileSizeBytes { get; set; }
    /// <summary>Presigned GET URL for the media file (R5). Empty when the file could not be presigned.</summary>
    public string? Url { get; set; }
    public string? ThumbnailUrl { get; set; }
    public DateTime? UploadedAt { get; set; }
    public long? UploadedByUserId { get; set; }
    public bool IsPrimary { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; }
}
