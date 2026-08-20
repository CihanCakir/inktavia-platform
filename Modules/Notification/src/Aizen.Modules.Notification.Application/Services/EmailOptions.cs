namespace Aizen.Modules.Notification.Application.Services;

public sealed class EmailOptions
{
    public const string SectionName = "Email";

    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 587;
    public string User { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FromAddress { get; set; } = "no-reply@inktavia.com";
    public string FromName { get; set; } = "Inktavia Marine";
    public bool UseSsl { get; set; } = true;

    /// <summary>
    /// Yanıt adresi. From = no-reply@inktavia.com olduğundan, yanıtlar bounce olmasın diye izlenen bir kutuya
    /// (info@inktavia.com) yönlendirilir. Boşsa Reply-To eklenmez.
    /// </summary>
    public string? ReplyTo { get; set; }
}
