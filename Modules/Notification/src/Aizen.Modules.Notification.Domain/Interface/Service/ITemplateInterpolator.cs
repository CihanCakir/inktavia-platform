namespace Aizen.Modules.Notification.Domain.Interface.Service;

public interface ITemplateInterpolator
{
    string Interpolate(string template, IReadOnlyDictionary<string, string> variables);
}
