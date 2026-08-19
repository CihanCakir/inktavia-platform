using Aizen.Core.Messagebus.Abstraction.Messages;

namespace Aizen.Modules.Identity.Abstraction.Message;

/// <summary>
/// İç mesaj kuyruğuna yayınlanan sağlayıcı e-posta doğrulama isteği. Notification modülü bunu
/// PROVIDER_EMAIL_VERIFICATION şablonuyla e-postaya çevirir. Token içeren ham <see cref="VerifyUrl"/>
/// yalnızca iç kuyrukta dolaşır — BFF'e/tarayıcıya asla dönmez (OTP ile aynı güven modeli).
/// </summary>
public sealed class ProviderEmailVerificationRequestedMessage : AizenBaseMessage
{
    public long RecipientUserId { get; set; }
    public string Email { get; set; } = default!;
    public string MaskedTarget { get; set; } = default!;
    public string VerifyUrl { get; set; } = default!;
    public int ExpiresInMinutes { get; set; }
}
