namespace Aizen.Modules.Identity.Abstraction.Dto.Common;

/// <summary>Faz 28.7 — profil id'sinden telefon çözümü (PII-minimal iç okuma; e-posta/dil çözümüyle aynı sınır).</summary>
public sealed class ProfilePhoneNumberDto
{
    public long    ProfileId   { get; set; }
    public string? PhoneNumber { get; set; }
}
