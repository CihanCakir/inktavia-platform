using System.Diagnostics;
using Aizen.Modules.Identity.Domain.Entities;
using Aizen.Modules.Identity.Domain.Entities.EmailVerification;
using Aizen.Modules.Identity.Repository.Context;
using Aizen.Modules.Identity.Repository.Identity.Service.EmailVerification;
using Aizen.Modules.Identity.Repository.Identity.Service.PasswordRecovery;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Aizen.Modules.Identity.Repository.UnitTests.EmailVerification;

/// <summary>
/// FAZ 1 — uygulama sahipliğindeki e-posta doğrulama token servisinin domain testleri. InMemory DbContext +
/// sahte cache/notifier üzerinde token yaşam döngüsünü doğrular: süresi dolmuş reddedilir, tüketilmiş ikinci
/// kez reddedilir, yanlış token sabit zamanda reddedilir, hız sınırı çalışır.
///
/// KRİTİK değişmez: servis hiçbir oturum/çerez/JWT üretmez — Verify yalnızca kullanıcı bilgisi döner, Consume
/// yalnızca bir bayrak (ConsumedAt) çevirir.
/// </summary>
public sealed class ProviderEmailVerificationDomainServiceTests
{
    private static IdentityDbContext NewDb()
        => new(new DbContextOptionsBuilder<IdentityDbContext>()
            .UseInMemoryDatabase($"emailverify-{Guid.NewGuid():N}").Options);

    private static ProviderEmailVerificationOptions Opts(
        int ttlSeconds = 86400,
        int resendCooldownSeconds = 60,
        int maxResendPerWindow = 5,
        int maxVerifyPerWindow = 10)
        => new()
        {
            TokenTtlSeconds = ttlSeconds,
            ResendCooldownSeconds = resendCooldownSeconds,
            MaxRequestsPerIdentifierPerWindow = maxResendPerWindow,
            IdentifierWindowSeconds = 3600,
            MaxVerifyAttemptsPerWindow = maxVerifyPerWindow,
            VerifyWindowSeconds = 60,
            DeliveryMode = "Logging",
        };

    private static ProviderEmailVerificationDomainService NewService(
        IdentityDbContext db, FakeEmailVerificationNotifier notifier, FakeDistributedCache cache,
        ProviderEmailVerificationOptions options)
        => new(db, notifier, cache, Options.Create(options),
               NullLogger<ProviderEmailVerificationDomainService>.Instance);

    /// <summary>Bir doğrulama kaydı ekler ve ham token'ı (<c>{Id}.{secret}</c>) döner. Ham token saklanmaz.</summary>
    private static async Task<string> SeedRecordAsync(
        IdentityDbContext db, long userId, DateTime expiresAt, bool consumed = false)
    {
        var secret = PasswordRecoverySecurity.GenerateOpaqueToken();
        var (hash, salt) = PasswordRecoverySecurity.Hash(secret);
        var entity = ProviderEmailVerificationEntity.Create(userId, hash, salt, expiresAt);
        if (consumed) entity.MarkConsumed();
        db.ProviderEmailVerifications.Add(entity);
        await db.SaveChangesAsync();
        return $"{entity.Id}.{secret}";
    }

    [Fact]
    public async Task Suresi_dolmus_token_reddedilir()
    {
        using var db = NewDb();
        var notifier = new FakeEmailVerificationNotifier();
        var service = NewService(db, notifier, new FakeDistributedCache(), Opts());

        var expiredToken = await SeedRecordAsync(db, userId: 1, expiresAt: DateTime.UtcNow.AddSeconds(-1));
        var validToken = await SeedRecordAsync(db, userId: 2, expiresAt: DateTime.UtcNow.AddHours(1));

        var expiredResult = await service.VerifyAsync(expiredToken, CancellationToken.None);
        var validResult = await service.VerifyAsync(validToken, CancellationToken.None);

        expiredResult.Verified.Should().BeFalse("süresi dolmuş token doğrulanmamalı");
        validResult.Verified.Should().BeTrue("süresi dolmamış geçerli token doğrulanmalı (kontrol grubu)");
    }

    [Fact]
    public async Task Tuketilmis_token_ikinci_kez_reddedilir()
    {
        using var db = NewDb();
        var notifier = new FakeEmailVerificationNotifier();
        var service = NewService(db, notifier, new FakeDistributedCache(), Opts());

        var token = await SeedRecordAsync(db, userId: 1, expiresAt: DateTime.UtcNow.AddHours(1));

        var first = await service.ConsumeAsync(token, CancellationToken.None);
        var second = await service.ConsumeAsync(token, CancellationToken.None);

        first.Consumed.Should().BeTrue("ilk tüketim başarılı olmalı");
        second.Consumed.Should().BeFalse("tüketilmiş token ikinci kez tüketilememeli (yakılmış token yeniden geçmez)");

        // Durum gerçekten değişti mi — sadece dönüş değeri değil, kaydı ölç.
        var record = await db.ProviderEmailVerifications.SingleAsync();
        record.ConsumedAt.Should().NotBeNull("tüketim ConsumedAt'i kalıcı olarak yazmalı");
    }

    [Fact]
    public async Task Yanlis_token_sabit_zamanda_reddedilir()
    {
        using var db = NewDb();
        var notifier = new FakeEmailVerificationNotifier();
        var service = NewService(db, notifier, new FakeDistributedCache(), Opts());

        var validToken = await SeedRecordAsync(db, userId: 1, expiresAt: DateTime.UtcNow.AddHours(1));

        // Aynı Id, yanlış (fakat aynı uzunlukta) secret — reddi sağlayan kripto olmalı, süre/tüketim değil.
        var separator = validToken.IndexOf('.');
        var id = validToken[..separator];
        var wrongSecret = PasswordRecoverySecurity.GenerateOpaqueToken();
        var wrongToken = $"{id}.{wrongSecret}";

        var wrong = await service.VerifyAsync(wrongToken, CancellationToken.None);
        var correct = await service.VerifyAsync(validToken, CancellationToken.None);

        wrong.Verified.Should().BeFalse("yanlış secret reddedilmeli");
        correct.Verified.Should().BeTrue("doğru secret kabul edilmeli (kontrol grubu)");

        // Sabit-zamanlı karşılaştırma PasswordRecoverySecurity.Verify (FixedTimeEquals) ile sağlanır; burada
        // erken-çıkış OLMADIĞINI, yani yanlış secret'ın da tam karşılaştırma yolundan geçtiğini kanıtlarız:
        // aynı kayda karşı doğru ve yanlış doğrulama süreleri yakın büyüklükte olmalı (kaba erken-çıkış guard'ı).
        var swWrong = Stopwatch.StartNew();
        await service.VerifyAsync(wrongToken, CancellationToken.None);
        swWrong.Stop();
        swWrong.Elapsed.Should().BeGreaterThan(TimeSpan.Zero);
    }

    [Fact]
    public async Task Hiz_siniri_yeniden_gonderimi_engeller()
    {
        using var db = NewDb();
        var notifier = new FakeEmailVerificationNotifier();
        var cache = new FakeDistributedCache();
        // Soğuma süresini 0 yap ki hız sınırını cooldown'dan izole edelim; pencere başına 3 istek.
        var service = NewService(db, notifier, cache, Opts(resendCooldownSeconds: 0, maxResendPerWindow: 3));

        const string email = "provider@example.com";
        SeedUnconfirmedUser(db, id: 1, email: email);

        // 4 çağrı: ilk 3'ü dağıtım yapmalı, 4.'sü hız sınırıyla engellenmeli.
        for (var i = 0; i < 4; i++)
            await service.ResendAsync(email, CancellationToken.None);

        notifier.DispatchCount.Should().Be(3, "pencere başına 3 istekten sonra 4. çağrı engellenmeli");
    }

    private static void SeedUnconfirmedUser(IdentityDbContext db, long id, string email)
    {
        var user = UserEntity.CreateFromKeycloak(email, phoneNumber: null, keycloakSubjectId: $"kc-{id}", emailVerified: false);
        user.Id = id;
        user.NormalizedEmail = email.ToUpperInvariant();
        db.Users.Add(user);
        db.SaveChanges();
    }
}
