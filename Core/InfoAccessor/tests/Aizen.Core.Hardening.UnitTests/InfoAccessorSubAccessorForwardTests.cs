using Aizen.Core.InfoAccessor.Abstraction;
using Aizen.Core.InfoAccessor.Extensions;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Aizen.Core.Hardening.UnitTests;

/// <summary>
/// Faz 28.1 regresyon: AddAizenInfoAccessor, IAizenClientInfoAccessor forward'ını kaydetmelidir.
/// Bu forward eksikken RecipientLocaleResolver (IAizenClientInfoAccessor enjekte eder) çözülemiyor ve
/// SendNotificationCommandHandler aktive edilemeyip TÜM bildirim gönderimleri (OTP e-postaları dahil) bloke oluyordu.
/// Test: IAizenClientInfoAccessor çözülür VE IAizenInfoAccessor.ClientInfoAccessor ile AYNI örnektir.
/// </summary>
public sealed class InfoAccessorSubAccessorForwardTests
{
    private static ServiceProvider BuildProvider()
    {
        // Development ortamı: BffAssertion secret zorunluluğu (fail-closed) devre dışı → minimal kurulum yeter.
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ASPNETCORE_ENVIRONMENT"] = "Development",
            })
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        // AizenInfoAccessor ctor'u IConfiguration ister → DI'a ekle.
        services.AddSingleton<IConfiguration>(config);
        services.AddAizenInfoAccessor(config);

        // validateScopes: scoped erişimcilerin yanlışlıkla root'tan çözülmesini erken yakalar.
        return services.BuildServiceProvider(validateScopes: true);
    }

    [Fact]
    public void AddAizenInfoAccessor_registers_ClientInfoAccessor_as_same_instance_as_InfoAccessor()
    {
        using var provider = BuildProvider();
        using var scope = provider.CreateScope();
        var sp = scope.ServiceProvider;

        var infoAccessor = sp.GetRequiredService<IAizenInfoAccessor>();

        // Forward kayıtlı değilse burası activation ile patlardı (bug'ın kök nedeni).
        var clientAccessor = sp.GetRequiredService<IAizenClientInfoAccessor>();

        clientAccessor.Should().NotBeNull();
        clientAccessor.Should().BeSameAs(infoAccessor.ClientInfoAccessor);
    }

    [Fact]
    public void UserInfoAccessor_forward_still_works()
    {
        // Mevcut forward'ın bozulmadığını da doğrula (regresyon güvencesi).
        using var provider = BuildProvider();
        using var scope = provider.CreateScope();
        var sp = scope.ServiceProvider;

        var infoAccessor = sp.GetRequiredService<IAizenInfoAccessor>();
        var userAccessor = sp.GetRequiredService<IAizenUserInfoAccessor>();

        userAccessor.Should().BeSameAs(infoAccessor.UserInfoAccessor);
    }
}
