using Aizen.Modules.Identity.Domain.Entities;
using Aizen.Modules.Identity.Domain.Model.EmailVerification;
using Aizen.Modules.Identity.Repository.Context;
using Aizen.Modules.Identity.Repository.Identity.Service.EmailVerification;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Aizen.Modules.Identity.Repository.UnitTests.EmailVerification;

/// <summary>
/// Phase 1 (revize) — ASP.NET Core Identity'nin YERLEŞİK e-posta onay token'ı üzerine kurulu servisin testleri.
/// GERÇEK UserManager + DataProtection harness'ı kullanılır (custom PBKDF2 yok): süresi dolmuş token reddedilir,
/// A'nın token'ı B'yi onaylamaz, bozuk token istisna fırlatmadan reddedilir, yeniden gönderme hız sınırı çalışır,
/// iki kez onaylamak güvenlidir. "Çağrı Success döndü" değil, EmailConfirmed'in gerçekten true olduğu ölçülür.
/// </summary>
public sealed class ProviderEmailVerificationDomainServiceTests
{
    private static readonly CancellationToken CT = CancellationToken.None;

    private sealed class Harness : IDisposable
    {
        public required ServiceProvider Sp { get; init; }
        public required UserManager<UserEntity> Users { get; init; }
        public required IOptions<DataProtectionTokenProviderOptions> TokenOptions { get; init; }
        public void Dispose() => Sp.Dispose();
    }

    private static Harness BuildHarness(TimeSpan tokenLifespan)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDataProtection();
        services.AddDbContext<IdentityDbContext>(o => o.UseInMemoryDatabase($"ev-{Guid.NewGuid():N}"));
        services.AddIdentityCore<UserEntity>(o => o.User.RequireUniqueEmail = true)
                .AddRoles<RoleEntity>()
                .AddEntityFrameworkStores<IdentityDbContext>()
                .AddDefaultTokenProviders();
        services.Configure<DataProtectionTokenProviderOptions>(o => o.TokenLifespan = tokenLifespan);

        // Süresi-dolmuş vs geçersiz ayrımı: DI ile aynı — uzun-ömürlü ikinci onay sağlayıcısı.
        services.Configure<LongLivedEmailConfirmationTokenProviderOptions>(_ => { });
        services.AddTransient<LongLivedEmailConfirmationTokenProvider<UserEntity>>();
        services.Configure<IdentityOptions>(o =>
            o.Tokens.ProviderMap[ProviderEmailVerificationDomainService.LongLivedEmailConfirmationProvider] =
                new TokenProviderDescriptor(typeof(LongLivedEmailConfirmationTokenProvider<UserEntity>)));

        var sp = services.BuildServiceProvider();
        return new Harness
        {
            Sp = sp,
            Users = sp.GetRequiredService<UserManager<UserEntity>>(),
            TokenOptions = sp.GetRequiredService<IOptions<DataProtectionTokenProviderOptions>>(),
        };
    }

    private static ProviderEmailVerificationDomainService NewService(
        Harness h, FakeEmailVerificationNotifier notifier, FakeDistributedCache cache,
        ProviderEmailVerificationOptions options)
        => new(h.Users, notifier, cache, Options.Create(options), h.TokenOptions,
               NullLogger<ProviderEmailVerificationDomainService>.Instance);

    private static ProviderEmailVerificationOptions Opts(int resendCooldown = 60, int maxPerWindow = 5)
        => new()
        {
            ResendCooldownSeconds = resendCooldown,
            MaxRequestsPerIdentifierPerWindow = maxPerWindow,
            IdentifierWindowSeconds = 3600,
            VerifyUrlTemplate = "https://provider.test/auth/verify-callback?token={0}",
            DeliveryMode = "Logging",
        };

    private static async Task<UserEntity> CreateUserAsync(Harness h, string email)
    {
        var user = UserEntity.CreateFromKeycloak(email, phoneNumber: null, keycloakSubjectId: $"kc-{Guid.NewGuid():N}", emailVerified: false);
        var res = await h.Users.CreateAsync(user);
        res.Succeeded.Should().BeTrue(string.Join("; ", res.Errors.Select(e => e.Description)));
        return user;
    }

    /// <summary>verifyUrl'den birleşik token'ı ({userId}.{Base64Url}) söker. Token URL-güvenlidir → '&'/'=' yok.</summary>
    private static string ExtractToken(string? verifyUrl)
    {
        verifyUrl.Should().NotBeNull();
        const string marker = "token=";
        var i = verifyUrl!.IndexOf(marker, StringComparison.Ordinal);
        i.Should().BeGreaterThanOrEqualTo(0);
        return verifyUrl[(i + marker.Length)..];
    }

    [Fact]
    public async Task Onay_EmailConfirmed_i_true_yapar_ve_ikinci_kez_guvenlidir()
    {
        using var h = BuildHarness(TimeSpan.FromHours(24));
        var user = await CreateUserAsync(h, "provider@example.com");
        var notifier = new FakeEmailVerificationNotifier();
        var service = NewService(h, notifier, new FakeDistributedCache(), Opts());

        await service.GenerateAsync("provider@example.com", CT);
        var token = ExtractToken(notifier.LastVerifyUrl);

        var first = await service.ConfirmAsync(token, CT);
        first.Confirmed.Should().BeTrue();
        first.KeycloakSubjectId.Should().NotBeNullOrEmpty("BFF Keycloak'ı çevirmek için subject'e ihtiyaç duyar");

        // "Success döndü" değil — durumu ölç: EmailConfirmed gerçekten kalıcı olarak true olmalı.
        var reloaded = await h.Users.FindByIdAsync(user.Id.ToString());
        reloaded!.EmailConfirmed.Should().BeTrue("ConfirmEmailAsync EmailConfirmed'i DB'ye yazmalı");

        // İkinci onay zararsız ve hatasız (idempotent).
        var second = await service.ConfirmAsync(token, CT);
        second.Confirmed.Should().BeTrue("ikinci onay hata vermemeli ve aynı sonucu dönmeli");
    }

    [Fact]
    public async Task A_kullanicisinin_tokeni_B_yi_onaylamaz()
    {
        using var h = BuildHarness(TimeSpan.FromHours(24));
        var a = await CreateUserAsync(h, "a@example.com");
        var b = await CreateUserAsync(h, "b@example.com");
        var notifier = new FakeEmailVerificationNotifier();
        var service = NewService(h, notifier, new FakeDistributedCache(), Opts());

        await service.GenerateAsync("a@example.com", CT);
        var tokenA = ExtractToken(notifier.LastVerifyUrl); // {a.Id}.{enc}

        // A'nın token payload'ını B'nin id'siyle birleştir (saldırı denemesi).
        var encoded = tokenA[(tokenA.IndexOf('.') + 1)..];
        var forged = $"{b.Id}.{encoded}";

        var result = await service.ConfirmAsync(forged, CT);
        result.Confirmed.Should().BeFalse("A için üretilen token B'yi onaylamamalı");
        result.Status.Should().Be(EmailVerificationConfirmStatus.Invalid,
            "yanlış kullanıcı = geçersiz (süresi dolmuş DEĞİL)");

        var bReloaded = await h.Users.FindByIdAsync(b.Id.ToString());
        bReloaded!.EmailConfirmed.Should().BeFalse();
        a.Id.Should().NotBe(b.Id);
    }

    [Fact]
    public async Task Bozuk_veya_kirpilmis_token_istisna_firlatmadan_reddedilir()
    {
        using var h = BuildHarness(TimeSpan.FromHours(24));
        await CreateUserAsync(h, "provider@example.com");
        var notifier = new FakeEmailVerificationNotifier();
        var service = NewService(h, notifier, new FakeDistributedCache(), Opts());

        await service.GenerateAsync("provider@example.com", CT);
        var valid = ExtractToken(notifier.LastVerifyUrl);
        var truncated = valid[..(valid.Length - 5)]; // geçerli token'ın kırpılmışı

        foreach (var bad in new[] { "", "garbage", "123", "123.", "123.@@@not-base64@@@", "abc.Zm9v", truncated })
        {
            var act = async () => await service.ConfirmAsync(bad, CT);
            var result = (await act.Should().NotThrowAsync($"'{bad}' istisna fırlatmamalı")).Subject;
            result.Confirmed.Should().BeFalse($"'{bad}' onaylanmamalı");
            result.Status.Should().Be(EmailVerificationConfirmStatus.Invalid, $"'{bad}' geçersiz olmalı");
        }
    }

    [Fact]
    public async Task Yeniden_gonderme_hiz_siniri_uygulanir()
    {
        using var h = BuildHarness(TimeSpan.FromHours(24));
        await CreateUserAsync(h, "provider@example.com"); // onaylanmamış
        var notifier = new FakeEmailVerificationNotifier();
        // Soğumayı 0 yapıp hız sınırını izole et; pencere başına 3 istek.
        var service = NewService(h, notifier, new FakeDistributedCache(), Opts(resendCooldown: 0, maxPerWindow: 3));

        for (var i = 0; i < 4; i++)
            await service.ResendAsync("provider@example.com", CT);

        notifier.DispatchCount.Should().Be(3, "pencere başına 3 istekten sonra 4. çağrı engellenmeli");
    }

    [Fact]
    public async Task Suresi_dolmus_token_reddedilir()
    {
        // Token ömrünü test için 1 ms'ye çek (istenen: TokenLifespan'i manipüle et).
        using var h = BuildHarness(TimeSpan.FromMilliseconds(1));
        var user = await CreateUserAsync(h, "provider@example.com");
        var notifier = new FakeEmailVerificationNotifier();
        var service = NewService(h, notifier, new FakeDistributedCache(), Opts());

        await service.GenerateAsync("provider@example.com", CT);
        var token = ExtractToken(notifier.LastVerifyUrl);

        await Task.Delay(50, CT); // token'ın süresi dolsun

        var result = await service.ConfirmAsync(token, CT);
        result.Confirmed.Should().BeFalse("süresi dolmuş token reddedilmeli");
        result.Status.Should().Be(EmailVerificationConfirmStatus.Expired,
            "süresi dolmuş, geçersizden AYIRT EDİLEBİLİR olmalı (FE 'yeniden gönder' düğmesi buna bağlı)");

        var reloaded = await h.Users.FindByIdAsync(user.Id.ToString());
        reloaded!.EmailConfirmed.Should().BeFalse("süresi dolmuş token EmailConfirmed'i true yapmamalı");
    }
}
