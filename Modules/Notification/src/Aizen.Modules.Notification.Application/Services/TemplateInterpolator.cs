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
}
