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

        // Direct registration — required by ProcessIyzicoWebhookCommandHandler
        services.AddScoped<IyzicoMarketplacePaymentGatewayProvider>();

        // ── Gateway resolver ──────────────────────────────────────────────────
        services.AddScoped<PaymentGatewayResolver>();

        // ── Commission calculator ─────────────────────────────────────────────
        services.AddScoped<CommissionCalculationService>();

        // ── Invoice number generator ──────────────────────────────────────────
        services.AddScoped<InvoiceNumberService>();

        // NOTE: Message consumers (ServiceRequestCompletedConsumer, ServiceRequestCancelledConsumer)
        // are registered in Aizen.Modules.Payment (web host) DI, not here.
        // Consumers must live in the host project — they inline business logic and must NOT
        // dispatch nested CQRS commands via ISender.

        // NOTE: Recurring jobs (PaymentEscrowTimeoutJob, PaymentReminderJob, PayoutProcessingJob,
        // StaleEscrowCleanupJob) are now in Aizen.Modules.Payment/Jobs/ and auto-discovered
        // by AizenApplicationBuilder via host assembly scanning.

        // CQRS handlers are auto-discovered by AizenApplicationBuilder
        // via assembly scanning when AppType.Scheduler is included in AizenAppInfo.TypeInclude.
        return services;
    }
}
