using Aizen.Modules.Notification.Domain.Interface.Service;
using System.Text.RegularExpressions;

namespace Aizen.Modules.Notification.Application.Services;

public sealed class TemplateInterpolator : ITemplateInterpolator
{
    private static readonly Regex _placeholder = new(@"\{\{(\w+)\}\}", RegexOptions.Compiled);

    public string Interpolate(string template, IReadOnlyDictionary<string, string> variables)
    {
        return _placeholder.Replace(template, match =>
        {
            var key = match.Groups[1].Value;
            return variables.TryGetValue(key, out var value) ? value : match.Value;
        });
    }

    public string? TryInterpolateStrict(
        string template,
        IReadOnlyDictionary<string, string> variables,
        out IReadOnlyList<string> missingKeys)
    {
        // Önce eksik anahtarları topla (distinct, ilk-görülme sırasını koru) — sonra ancak hepsi mevcutsa render et.
        List<string>? missing = null;
        foreach (Match m in _placeholder.Matches(template))
        {
            var key = m.Groups[1].Value;
            if (!variables.ContainsKey(key))
            {
                missing ??= new List<string>();
                if (!missing.Contains(key))
                    missing.Add(key);
            }
        }

        if (missing is { Count: > 0 })
        {
            missingKeys = missing;
            return null;
        }

        missingKeys = Array.Empty<string>();
        return _placeholder.Replace(template, match => variables[match.Groups[1].Value]);
    }
}
