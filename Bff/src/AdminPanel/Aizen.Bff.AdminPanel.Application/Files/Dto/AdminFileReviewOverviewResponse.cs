using Aizen.Bff.AdminPanel.Application.Common;
using Aizen.Bff.AdminPanel.Application.Common.Warnings;

namespace Aizen.Bff.AdminPanel.Application.Files.Dto;

[DocumentationInfo("Admin file review overview response", "File metadata for the admin file review screen.")]
public sealed class AdminFileReviewOverviewResponse
{
    public FileMetadataResult? FileMetadata { get; set; }
    public List<AdminBffWarning> Warnings { get; set; } = new();
}
