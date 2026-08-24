using Aizen.Core.InfoAccessor.Abstraction;
using Aizen.Core.Infrastructure.Api;
using Aizen.Modules.Notification.Abstraction.RemoteCall;
using Aizen.Modules.Notification.Application.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Aizen.Modules.Notification.Application.UnitTests;

/// <summary>
/// RecipientLocaleResolver'ın öncelik zincirini kanıtlar: (1) kalıcı tercih → (2) istek bağlamı → (3) varsayılan.
///   • Geçerli kalıcı tercih her şeyi ezer.
///   • Boş tercih → istek bağlamındaki (ClientInfo) geçerli koda düşer.
///   • Allowlist dışı kod (tercih VEYA istek) yok sayılır; bir sonraki geçerli kaynağa düşülür.
///   • Hiçbir kaynak geçerli değilse NotificationOptions.DefaultLocale döner.
///   • Identity remote-call hatası best-effort yutulur; çözüm istek/varsayılana düşer.
/// Doğrulama konfigürasyon allowlist'ine göre yapılır — ReferenceData'ya gidilmez.
/// </summary>
public sealed class RecipientLocaleResolverTests
{
    private const long RecipientProfileId = 5001;

    // Kanıtlanan kodu döndüren sahte Identity remote-call (opsiyonel olarak patlar).
    private sealed class FakeIdentity : INotificationIdentityRemoteCall
    {
        private readonly string? _preferred;
        private readonly bool _throw;

        public FakeIdentity(string? preferred, bool @throw = false)
        {
            _preferred = preferred;
            _throw = @throw;
        }

        public Task<AizenApiResponse<ProfilePreferredLanguageResult>> GetProfilePreferredLanguage(long profileId)
        {
            if (_throw) throw new InvalidOperationException("identity down");
            return Task.FromResult(new AizenApiResponse<ProfilePreferredLanguageResult>
            {
                Body = new ProfilePreferredLanguageResult { ProfileId = profileId, PreferredLanguage = _preferred }
            });
        }

        // Bu testlerde kullanılmayan yollar.
        public Task<AizenApiResponse<List<ProviderForAreaResult>>> GetProvidersForArea(string cityCode, string? categoryCode = null, int take = 500) => throw new NotImplementedException();
        public Task<AizenApiResponse<List<long>>> GetAdminUserIds() => throw new NotImplementedException();
        public Task<AizenApiResponse<ParticipantProfileIdResult>> GetParticipantProfileIdByUserId(long userId) => throw new NotImplementedException();
        public Task<AizenApiResponse<ProfileContactEmailResult>> GetProfileContactEmail(long profileId) => throw new NotImplementedException();
    }

    private sealed class FakeClientInfo : IAizenClientInfoAccessor
    {
        public FakeClientInfo(string language) => ClientInfo = new AizenClientInfo { Language = language };
        public AizenClientInfo ClientInfo { get; }
    }

    private static RecipientLocaleResolver Build(string? preferred, string requestLanguage, bool identityThrows = false, NotificationOptions? options = null)
        => new(
            new FakeIdentity(preferred, identityThrows),
            new FakeClientInfo(requestLanguage),
            Options.Create(options ?? new NotificationOptions()),
            NullLogger<RecipientLocaleResolver>.Instance);

    [Fact]
    public async Task Valid_persisted_preference_wins_over_request_and_default()
    {
        // İstek "tr" olsa da kalıcı tercih "en" → tercih kazanır.
        var sut = Build(preferred: "en", requestLanguage: "tr");

        var locale = await sut.ResolveAsync(RecipientProfileId);

        locale.Should().Be("en");
    }

    [Fact]
    public async Task Persisted_preference_is_normalized_from_region_coded_value()
    {
        // "EN-us" → bölge eki atılıp küçük harfe indirgenir → "en".
        var sut = Build(preferred: "EN-us", requestLanguage: "tr");

        (await sut.ResolveAsync(RecipientProfileId)).Should().Be("en");
    }

    [Fact]
    public async Task Empty_preference_falls_back_to_valid_request_language()
    {
        // Tercih boş → istek bağlamındaki "en-US" normalize edilip ("en") kullanılır.
        var sut = Build(preferred: null, requestLanguage: "en-US,en;q=0.9");

        (await sut.ResolveAsync(RecipientProfileId)).Should().Be("en");
    }

    [Fact]
    public async Task Invalid_preference_code_is_ignored_and_falls_through_to_request()
    {
        // "de" allowlist (tr/en) dışında → yok sayılır, geçerli istek diline ("tr") düşülür.
        var sut = Build(preferred: "de", requestLanguage: "tr");

        var locale = await sut.ResolveAsync(RecipientProfileId);

        locale.Should().Be("tr");
        locale.Should().NotBe("de");
    }

    [Fact]
    public async Task No_valid_source_returns_configured_default()
    {
        // Tercih boş + istek dili allowlist dışı ("de-DE") → varsayılan "tr".
        var sut = Build(preferred: null, requestLanguage: "de-DE");

        (await sut.ResolveAsync(RecipientProfileId)).Should().Be("tr");
    }

    [Fact]
    public async Task Custom_default_locale_is_honored_when_nothing_else_valid()
    {
        var options = new NotificationOptions { DefaultLocale = "en", AllowedLocales = new() { "tr", "en" } };
        var sut = Build(preferred: null, requestLanguage: "fr", options: options);

        (await sut.ResolveAsync(RecipientProfileId)).Should().Be("en");
    }

    [Fact]
    public async Task Identity_failure_is_swallowed_and_resolution_falls_back()
    {
        // Remote-call patlar → best-effort yutulur, geçerli istek diline ("en") düşülür.
        var sut = Build(preferred: "en", requestLanguage: "en", identityThrows: true);

        (await sut.ResolveAsync(RecipientProfileId)).Should().Be("en");
    }

    [Fact]
    public async Task Non_positive_recipient_id_skips_identity_and_uses_request_or_default()
    {
        // id <= 0 → Identity'ye hiç gidilmez (FakeIdentity patlasa bile), istek diline düşülür.
        var sut = Build(preferred: "en", requestLanguage: "en", identityThrows: true);

        (await sut.ResolveAsync(0)).Should().Be("en");
    }
}
