using Aizen.Core.Infrastructure.Exception;
using Microsoft.Extensions.Configuration;

namespace Aizen.Modules.Content.Application.Services;

/// <summary>
/// Config-backed supported-language check (B4 fallback — see <see cref="ILanguageValidator"/>).
/// Reads Content:Languages:Supported (default { "tr", "en" }); comparison is case-insensitive.
/// </summary>
public sealed class LanguageValidator : ILanguageValidator
{
    private static readonly string[] DefaultSupported = { "tr", "en" };

    private readonly HashSet<string> _supported;

    public LanguageValidator(IConfiguration configuration)
    {
        var configured = configuration.GetSection("Content:Languages:Supported").Get<string[]>();
        var source = configured is { Length: > 0 } ? configured : DefaultSupported;
        _supported = new HashSet<string>(source, StringComparer.OrdinalIgnoreCase);
    }

    public void EnsureSupported(string languageCode)
    {
        if (string.IsNullOrWhiteSpace(languageCode) || !_supported.Contains(languageCode))
            throw new AizenBusinessException(
                $"Unsupported language code '{languageCode}'. Supported: {string.Join(", ", _supported)}.");
    }
}
