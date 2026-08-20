using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Aizen.Modules.Identity.Repository.Identity.Service.EmailVerification;

/// <summary>
/// Süresi-dolmuş ile geçersiz token'ı ayırt etmek için kullanılan uzun-ömürlü e-posta onay sağlayıcısı.
/// Yerleşik "Default" sağlayıcı tek bir başarısızlık döndürür (neden vermez); bu sağlayıcı AYNI DataProtection
/// amacına (<see cref="DataProtectionTokenProviderOptions.Name"/> = "DataProtectorTokenProvider") sahip olduğundan
/// varsayılan sağlayıcının ÜRETTİĞİ token'ı çözebilir — tek fark ~10 yıllık ömürdür. Böylece:
/// varsayılanda başarısız + burada geçerli ⇒ SÜRESİ DOLMUŞ; ikisinde de geçersiz ⇒ token bozuk/kurcalanmış
/// veya stamp uyuşmuyor ⇒ GEÇERSİZ. Durum (tablo) eklemeden çalışır.
/// </summary>
public sealed class LongLivedEmailConfirmationTokenProviderOptions : DataProtectionTokenProviderOptions
{
    public LongLivedEmailConfirmationTokenProviderOptions()
    {
        // Varsayılan sağlayıcıyla AYNI protector amacı — aksi halde token'ı çözemez.
        Name = "DataProtectorTokenProvider";
        // Pratikte "sınırsız": ömür kontrolünü etkisiz bırakır ki yalnızca imza/amaç/stamp doğrulansın.
        TokenLifespan = TimeSpan.FromDays(3650);
    }
}

public sealed class LongLivedEmailConfirmationTokenProvider<TUser> : DataProtectorTokenProvider<TUser>
    where TUser : class
{
    public LongLivedEmailConfirmationTokenProvider(
        IDataProtectionProvider dataProtectionProvider,
        IOptions<LongLivedEmailConfirmationTokenProviderOptions> options,
        ILogger<DataProtectorTokenProvider<TUser>> logger)
        : base(dataProtectionProvider, options, logger)
    {
    }
}
