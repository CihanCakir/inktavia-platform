using Aizen.Modules.Payment.Abstraction.Interface;
using Aizen.Modules.Payment.Application.Gateway.Iyzico;
using Aizen.Modules.Payment.Application.Services;
using Aizen.Modules.ReferenceData.Domain.Interface.Service;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Aizen.Modules.Payment.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddPaymentApplication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // ── Iyzico configuration ───────────────────────────────────────────────
        services.Configure<IyzicoConfiguration>(
            configuration.GetSection("Iyzico"));

        // ── Iyzico typed HTTP client ───────────────────────────────────────────
        services.AddHttpClient<IyzicoHttpClient>((sp, client) =>
        {
            var cfg = sp.GetRequiredService<IOptions<IyzicoConfiguration>>().Value;
            client.BaseAddress = new Uri(
                string.IsNullOrWhiteSpace(cfg.BaseUrl)
                    ? "https://sandbox-api.iyzipay.com"
                    : cfg.BaseUrl);
            client.Timeout = TimeSpan.FromSeconds(30);
            client.DefaultRequestHeaders.Add("Accept", "application/json");
        });

        // ── Gateway providers (keyed DI — swap via PAYMENT_GATEWAY_ACTIVE) ────
        services.AddKeyedScoped<IPaymentGatewayProvider, ManualPaymentGatewayProvider>("manual");
        services.AddKeyedScoped<IPaymentGatewayProvider, IyzicoMarketplacePaymentGatewayProvider>("iyzico");

        // Direct registration for PaymentWebhookController injection
        services.AddScoped<IyzicoMarketplacePaymentGatewayProvider>();

        // ── Gateway resolver ──────────────────────────────────────────────────
        services.AddScoped<PaymentGatewayResolver>();

        // ── Commission calculator ─────────────────────────────────────────────
        services.AddScoped<CommissionCalculationService>();

        // ── Message consumers ─────────────────────────────────────────────────
        // Registered here so AizenApplicationBuilder's consumer discovery
        // (AppType.Worker) can resolve them from DI.
        services.AddScoped<Consumers.ServiceRequestCompletedConsumer>();
        services.AddScoped<Consumers.ServiceRequestCancelledConsumer>();

        // CQRS handlers and recurring jobs are auto-discovered by AizenApplicationBuilder
        // via assembly scanning when AppType.Scheduler is included in AizenAppInfo.TypeInclude.
        return services;
    }
}
