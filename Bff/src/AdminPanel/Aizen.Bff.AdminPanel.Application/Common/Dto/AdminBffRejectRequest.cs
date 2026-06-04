namespace Aizen.Bff.AdminPanel.Application.Common.Dto;

[DocumentationInfo("Admin BFF reject request", "Request body carrying a rejection reason for admin profile rejection endpoints.")]
public sealed class AdminBffRejectRequest
{
    public string Reason { get; set; } = string.Empty;
}
