using Aizen.Core.Common.Abstraction.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Aizen.Core.Starter.Abstraction.Configuration;

/// <summary>
/// FAZ14 (#30) — "Doldurulmamış yapılandırmayla açılmayı reddet".
///
/// Chart values / appsettings içindeki <c>__FROM_ENV__</c> ve <c>__FROM_SECRET__</c> yer-tutucuları BOŞ değildir
/// (dolu birer string). Bu yüzden <c>string.IsNullOrWhiteSpace</c> gibi kontroller onları "dolu" sanır ve servis,
/// çöp değerle ayağa kalkar; sorun saatler sonra bir iş hatasına (ör. Keycloak redirect_uri=__FROM_ENV__ → 400)
/// dönüşür (borç #50/#53/#54/#57/#58/#61/#62 hep bu desen).
///
/// Bu koruma, host açılırken (env override'ları uygulandıktan SONRA) built <see cref="IConfiguration"/>'ı gezer ve
/// hâlâ bir yer-tutucuya EŞİT her değeri yakalar. "Önemli anahtar" listesi TUTULMAZ — yer-tutucunun kendisi sinyaldir.
///
///   • Development DIŞINDA: açılışı DURDURUR ve suçlu TÜM anahtarları tek mesajda listeler (fail-closed).
///   • Development'ta: her birini Error seviyesinde loglar ve açılışa devam eder (yerelde tıkanmamak için kaçış).
///
/// Precedent: Core/InfoAccessor/.../BuilderExtensions.cs — Development kaçışı + Development dışı fail-closed.
/// </summary>
public static class AizenConfigurationPlaceholderGuard
{
    // Kanonik yer-tutucu sabitleri Common.Abstraction'da; burada yalnız yeniden dışa aktarılır (tek kaynak).
    public const string EnvPlaceholder = AizenConfigPlaceholders.FromEnv;
    public const string SecretPlaceholder = AizenConfigPlaceholders.FromSecret;

    /// <summary>Yer-tutucuya eşit tek bir konfig girdisi: tam yol (ör. "Vapid:PrivateKey") + hangi yer-tutucu.</summary>
    public readonly record struct PlaceholderHit(string Path, string Token);

    /// <summary>
    /// Built config'i gezip hâlâ bir yer-tutucuya eşit olan TÜM anahtarları döndürür. Saf/yan-etkisiz — testten
    /// doğrudan çağrılabilir. "Önemli" anahtar seçmez; değer <c>__FROM_ENV__</c>/<c>__FROM_SECRET__</c> ise yakalar.
    /// </summary>
    public static IReadOnlyList<PlaceholderHit> Scan(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        var hits = new List<PlaceholderHit>();
        Collect(configuration, hits);
        return hits;
    }

    private static void Collect(IConfiguration node, List<PlaceholderHit> hits)
    {
        foreach (var child in node.GetChildren())
        {
            var value = child.Value;
            if (value is EnvPlaceholder or SecretPlaceholder)
                hits.Add(new PlaceholderHit(child.Path, value));

            // Alt bölümlere in — yaprak olmayan düğümlerin Value'su null'dur, yalnızca yapraklarda değer olur.
            Collect(child, hits);
        }
    }

    /// <summary>
    /// FAZ14 kapısı. Development dışında doldurulmamış yer-tutucu varsa <see cref="AizenConfigurationException"/>
    /// fırlatarak açılışı durdurur (TÜM suçlu anahtarlar tek mesajda); Development'ta her birini Error loglayıp
    /// devam eder. Hiç yer-tutucu yoksa sessizce döner.
    /// </summary>
    /// <param name="logger">
    /// Development log hedefi. Host açılış anında (Configure) DI logger'ı henüz yoktur; bu durumda null geçilir ve
    /// mesaj <see cref="Console.Error"/>'a yazılır. Test, Error seviyesinde loglandığını kanıtlamak için gerçek bir
    /// logger enjekte edebilir.
    /// </param>
    public static void EnsureNoUnfilledPlaceholders(
        IConfiguration configuration,
        IHostEnvironment environment,
        ILogger? logger = null)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(environment);

        var hits = Scan(configuration);
        if (hits.Count == 0)
            return;

        if (environment.IsDevelopment())
        {
            foreach (var hit in hits)
            {
                var line = BuildLine(hit, environment.EnvironmentName);
                if (logger is not null)
                    logger.LogError("{ConfigPlaceholderWarning}", line);
                else
                    Console.Error.WriteLine(line);
            }
            return;
        }

        throw new AizenConfigurationException(BuildFatalMessage(hits, environment.EnvironmentName));
    }

    // ── Mesajlar (§0.2 kalite çıtası: anahtarı, ortamı ve NASIL sağlanacağını adıyla söyle) ──────────────────

    private static string BuildFatalMessage(IReadOnlyList<PlaceholderHit> hits, string environmentName)
    {
        var lines = hits.Select(h => "  • " + BuildLine(h, environmentName));
        return
            $"Yapılandırma doldurulmadığı için servis '{environmentName}' ortamında güvenle başlatılamıyor " +
            $"(FAZ14 #30). Aşağıdaki {hits.Count} anahtar hâlâ yer-tutucu değeriyle duruyor ve doldurulmalıdır:" +
            Environment.NewLine + string.Join(Environment.NewLine, lines);
    }

    private static string BuildLine(PlaceholderHit hit, string environmentName)
    {
        // Konfig yolu ':' ayırıcılıdır; ortam değişkeni karşılığı '__' ile yazılır (ASP.NET Core kuralı).
        var envVar = hit.Path.Replace(":", "__");
        var howTo = hit.Token == SecretPlaceholder
            ? $"mühürlü 'platform-credentials' secret'ına ekleyip chart values-<ortam>.yaml içinde secretKeyRef ile '{envVar}' olarak bağlayın"
            : $"chart values-<ortam>.yaml içinde env girişi olarak '{envVar}' verin (yerelde docker-compose.yaml / .env üzerinden)";

        return $"'{hit.Path}' = '{hit.Token}' [ortam: {environmentName}] → {howTo}.";
    }
}
