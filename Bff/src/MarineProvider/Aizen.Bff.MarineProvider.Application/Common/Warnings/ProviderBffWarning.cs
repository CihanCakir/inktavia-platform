namespace Aizen.Bff.MarineProvider.Application.Common.Warnings;

/// <summary>
/// Non-fatal warning surfaced in an aggregated/orchestrated provider BFF response
/// (e.g. a downstream module call failed, or a capability is not yet available in this phase).
/// </summary>
public sealed class ProviderBffWarning
{
    public string Source { get; }
    public string Message { get; }

    public ProviderBffWarning(string source, string message)
    {
        Source = source;
        Message = message;
    }

    public static ProviderBffWarning Gap(string source, string detail) => new(source, detail);

    public static ProviderBffWarning CallFailed(string source, string reason) =>
        new(source, $"{source} call failed: {reason}");
}
