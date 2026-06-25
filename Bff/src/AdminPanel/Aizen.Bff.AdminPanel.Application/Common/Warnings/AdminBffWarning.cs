namespace Aizen.Bff.AdminPanel.Application.Common.Warnings;

[DocumentationInfo("Admin BFF warning", "Represents a non-fatal warning from a downstream module call within an aggregated BFF response.")]
public sealed class AdminBffWarning
{
    public string Module { get; }
    public string Message { get; }

    public AdminBffWarning(string module, string message)
    {
        Module = module;
        Message = message;
    }

    public static AdminBffWarning ModuleUnavailable(string module) =>
        new(module, $"{module} service is currently unavailable.");

    public static AdminBffWarning CallFailed(string module, string reason) =>
        new(module, $"{module} call failed: {reason}");
}
