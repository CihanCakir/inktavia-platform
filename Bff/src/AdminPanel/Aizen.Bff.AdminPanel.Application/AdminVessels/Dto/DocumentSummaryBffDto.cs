using Aizen.Bff.AdminPanel.Application.Common;

namespace Aizen.Bff.AdminPanel.Application.AdminVessels.Dto;

[DocumentationInfo("Document summary BFF DTO", "Summary of a vessel document for the detail page overview.")]
public sealed class DocumentSummaryBffDto
{
    public long Id { get; set; }
    public string? DocumentType { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public int? DaysUntilExpiry { get; set; }
    public string? DocumentStatus { get; set; }
}
