namespace Aizen.Modules.Notification.Domain.Exceptions;

/// <summary>
/// Strict interpolation: bir şablonda geçen {{placeholder}} anahtarları variables sözlüğünde YOKSA atılır.
/// eski davranış sessizce bırakıyordu — bilerek sıkılaştırıldı (eksik değişkenler artık gürültülü hata verir).
/// </summary>
public sealed class TemplatePlaceholderMissingException : Exception
{
    public string TemplateCode { get; }
    public IReadOnlyList<string> MissingKeys { get; }

    public TemplatePlaceholderMissingException(string templateCode, IReadOnlyList<string> missingKeys)
        : base($"Template '{templateCode}' render failed — missing variables for placeholder(s): " +
               $"{string.Join(", ", missingKeys)}.")
    {
        TemplateCode = templateCode;
        MissingKeys = missingKeys;
    }
}
