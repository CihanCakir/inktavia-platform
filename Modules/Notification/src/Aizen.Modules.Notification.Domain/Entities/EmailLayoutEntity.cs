using Aizen.Core.Domain;

namespace Aizen.Modules.Notification.Domain.Entities;

/// <summary>
/// E-posta gövdesini saran HTML kabuğu (header/footer/marka). İçerik render'ı, kanal içeriğinin HtmlTemplate çıktısını
/// bu kabuğun {{content}} yer tutucusuna gömer. HtmlShell MUTLAKA {{content}} içermelidir (Create doğrular).
/// </summary>
public sealed class EmailLayoutEntity : AizenEntity
{
    public const string ContentPlaceholder = "{{content}}";

    public string  Code      { get; private set; } = default!;
    public string  Name      { get; private set; } = default!;
    public string  HtmlShell { get; private set; } = default!;
    public bool    IsActive  { get; private set; }

    public DateTimeOffset  CreatedAt { get; private set; }
    public DateTimeOffset? UpdatedAt { get; private set; }

    private EmailLayoutEntity() { }

    public static EmailLayoutEntity Create(string code, string name, string htmlShell)
    {
        // Değişmez: kabuk {{content}} içermeli, yoksa render edilen gövde hiçbir yere gömülemez.
        if (string.IsNullOrWhiteSpace(htmlShell) || !htmlShell.Contains(ContentPlaceholder, StringComparison.Ordinal))
            throw new ArgumentException(
                $"Email layout HtmlShell must contain the {ContentPlaceholder} placeholder.", nameof(htmlShell));

        return new EmailLayoutEntity
        {
            Code      = code.ToUpperInvariant(),
            Name      = name,
            HtmlShell = htmlShell,
            IsActive  = true,
            CreatedAt = DateTimeOffset.UtcNow,
        };
    }

    public void SetActive(bool isActive)
    {
        IsActive  = isActive;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
