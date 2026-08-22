using Aizen.Core.Starter.Abstraction.Configuration;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Aizen.Core.Hardening.UnitTests;

/// <summary>
/// FAZ14 (#30) — "Doldurulmamış yapılandırmayla açılmayı reddet" korumasının davranışını kanıtlar:
///   • Development DIŞINDA bir yer-tutucu varsa açılış TÜM suçlu anahtarları içeren bir hatayla durur;
///   • aynı config Development'ta hata fırlatmadan açılır ve her suçlu anahtar Error seviyesinde loglanır;
///   • yer-tutucu yoksa her iki ortamda da sessizce (hatasız, logsuz) geçer;
///   • yer-tutucu env override ile GERÇEK bir değere doldurulduğunda artık yakalanmaz.
/// </summary>
public sealed class ConfigurationPlaceholderGuardTests
{
    private static IConfiguration BuildConfig(params (string Key, string Value)[] pairs) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(pairs.Select(p => new KeyValuePair<string, string?>(p.Key, p.Value)))
            .Build();

    private sealed class FakeEnvironment(string environmentName) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = environmentName;
        public string ApplicationName { get; set; } = "Aizen.Tests";
        public string ContentRootPath { get; set; } = ".";
        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; } = null!;
    }

    // Test için Error kayıtlarını toplayan minik logger.
    private sealed class CapturingLogger : ILogger
    {
        public readonly List<string> Errors = new();
        public IDisposable BeginScope<TState>(TState state) where TState : notnull => NullScope.Instance;
        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (logLevel == LogLevel.Error)
                Errors.Add(formatter(state, exception));
        }

        private sealed class NullScope : IDisposable
        {
            public static readonly NullScope Instance = new();
            public void Dispose() { }
        }
    }

    [Fact]
    public void Outside_development_throws_and_lists_every_offending_key()
    {
        var config = BuildConfig(
            ("Vapid:PrivateKey", AizenConfigurationPlaceholderGuard.EnvPlaceholder),
            ("Vapid:PublicKey", AizenConfigurationPlaceholderGuard.EnvPlaceholder),
            ("ContactIntake:IpHashSalt", AizenConfigurationPlaceholderGuard.SecretPlaceholder),
            ("Some:RealValue", "ok"));

        var act = () => AizenConfigurationPlaceholderGuard.EnsureNoUnfilledPlaceholders(
            config, new FakeEnvironment(Environments.Production));

        var ex = act.Should().Throw<AizenConfigurationException>(
            "Development dışında doldurulmamış konfig açılışı durdurmalı").Which;

        // TEK mesaj, TÜM suçlu anahtarları adıyla içermeli; ortamı ve nasıl sağlanacağını söylemeli.
        ex.Message.Should().Contain("Vapid:PrivateKey");
        ex.Message.Should().Contain("Vapid:PublicKey");
        ex.Message.Should().Contain("ContactIntake:IpHashSalt");
        ex.Message.Should().Contain(Environments.Production);
        ex.Message.Should().Contain("Vapid__PrivateKey", "ortam-değişkeni karşılığı '__' ile verilmeli");
        ex.Message.Should().Contain("secretKeyRef", "__FROM_SECRET__ için mühürlü secret yolu gösterilmeli");
        ex.Message.Should().NotContain("Some:RealValue", "gerçek değerli anahtarlar suçlanmamalı");
    }

    [Fact]
    public void In_development_logs_each_offender_at_error_level_and_does_not_throw()
    {
        var config = BuildConfig(
            ("Vapid:PrivateKey", AizenConfigurationPlaceholderGuard.EnvPlaceholder),
            ("ContactIntake:IpHashSalt", AizenConfigurationPlaceholderGuard.SecretPlaceholder));
        var logger = new CapturingLogger();

        var act = () => AizenConfigurationPlaceholderGuard.EnsureNoUnfilledPlaceholders(
            config, new FakeEnvironment(Environments.Development), logger);

        act.Should().NotThrow("Development'ta koruma açılışı engellememeli");
        logger.Errors.Should().HaveCount(2, "her suçlu anahtar Error seviyesinde loglanmalı");
        logger.Errors.Should().Contain(l => l.Contains("Vapid:PrivateKey"));
        logger.Errors.Should().Contain(l => l.Contains("ContactIntake:IpHashSalt"));
    }

    [Fact]
    public void No_placeholder_starts_silently_in_both_environments()
    {
        var config = BuildConfig(
            ("Vapid:PrivateKey", "real-key"),
            ("ContactIntake:IpHashSalt", "real-salt"),
            ("Keycloak:Authority", "https://auth.inktavia.com/realms/inktavia-realm"));
        var devLogger = new CapturingLogger();

        var prod = () => AizenConfigurationPlaceholderGuard.EnsureNoUnfilledPlaceholders(
            config, new FakeEnvironment(Environments.Production));
        var dev = () => AizenConfigurationPlaceholderGuard.EnsureNoUnfilledPlaceholders(
            config, new FakeEnvironment(Environments.Development), devLogger);

        prod.Should().NotThrow("yer-tutucu yoksa Production'da da sessizce açılmalı");
        dev.Should().NotThrow();
        devLogger.Errors.Should().BeEmpty("yer-tutucu yoksa Development'ta da log çıkmamalı");
    }

    [Fact]
    public void Placeholder_overridden_by_a_real_env_value_is_no_longer_flagged()
    {
        // appsettings katmanı placeholder koyar; env katmanı gerçek değerle EZER → built config'te artık yok.
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["MarineMobileKeycloak:BffRedirectUri"] = AizenConfigurationPlaceholderGuard.EnvPlaceholder,
            })
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["MarineMobileKeycloak:BffRedirectUri"] = "https://mapi.inktavia.com/auth/callback",
            })
            .Build();

        AizenConfigurationPlaceholderGuard.Scan(config).Should().BeEmpty(
            "override sonrası gerçek değer var; koruma yalnızca built (override sonrası) config'e bakar");

        var act = () => AizenConfigurationPlaceholderGuard.EnsureNoUnfilledPlaceholders(
            config, new FakeEnvironment(Environments.Production));
        act.Should().NotThrow();
    }
}
