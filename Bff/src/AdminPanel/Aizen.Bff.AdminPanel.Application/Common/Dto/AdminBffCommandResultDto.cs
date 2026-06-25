namespace Aizen.Bff.AdminPanel.Application.Common.Dto;

[DocumentationInfo("Admin BFF command result", "Standard result DTO returned by BFF command handlers.")]
public sealed class AdminBffCommandResultDto
{
    public bool Success { get; }
    public string? Message { get; }

    public AdminBffCommandResultDto(bool success, string? message = null)
    {
        Success = success;
        Message = message;
    }

    public static AdminBffCommandResultDto Ok(string? message = null) => new(true, message);
    public static AdminBffCommandResultDto Fail(string message) => new(false, message);
}
