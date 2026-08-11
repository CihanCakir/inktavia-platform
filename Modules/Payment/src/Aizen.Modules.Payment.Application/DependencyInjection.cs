using Aizen.Modules.Payment.Abstraction.Interface;
using Aizen.Modules.Payment.Application.Gateway.Apple;
using Aizen.Modules.Payment.Application.Gateway.Google;
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

        // ── BE-P9 auth-mode policy (Capture default / PreAuth per category) ────
        services.Configure<Gateway.PaymentAuthModeOptions>(
            configuration.GetSection(Gateway.PaymentAuthModeOptions.SectionName));

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

        // ── Platform fee calculator (BE-P3) ───────────────────────────────────
        // Remote-call adapter for ISystemParameterReferenceService (the DB-backed impl lives in ReferenceData and is
        // unusable in-process; PlatformFee/Commission read fee/VAT params from reference-data-api). Reads delegated,
        // writes NotSupported. The IPaymentReferenceDataRemoteCall client itself is auto-registered by AddAizenRemoteCall.
        services.AddScoped<ISystemParameterReferenceService, SystemParameterReferenceRemoteService>();

        services.AddScoped<PlatformFeeCalculationService>();

        // ── Profit protection engine wiring (BE-P5) ───────────────────────────
        services.AddScoped<ProfitProtectionCalculationService>();

        // ── Customer discount + benefit budget (BE-P6) ────────────────────────
        services.AddScoped<CustomerDiscountBenefitService>();
        services.AddScoped<CustomerBenefitBudgetService>();

        // ── Provider commission benefit (BE-P7) ───────────────────────────────
        services.AddScoped<ProviderCommissionBenefitService>();
        services.AddScoped<CommissionBenefitEntitlementService>();

        // ── SR acceptance economics combiner (BE-P8) ──────────────────────────
        services.AddScoped<ServiceRequestPaymentEconomicsCalculationService>();

        // ── Refund allocation orchestration (BE-P10) ──────────────────────────
        services.AddScoped<RefundAllocationService>();
        services.AddScoped<ProviderNegativeBalanceService>();

        // ── Premium offer-boost orchestration (BE-P11) ────────────────────────
        services.AddScoped<PremiumBoostService>();

        // ── Financial reporting ledger posting + backfill (BE-P12) ────────────
        services.AddScoped<FinancialLedgerPostingService>();
        services.AddScoped<FinancialLedgerBackfillService>();

        // ── Invoice number generator ──────────────────────────────────────────
        services.AddScoped<InvoiceNumberService>();

        // ── CargoDry settlement payout service (Phase 4B) ─────────────────────
        // Bridges CargoDry → Payment module boundary in-process.
        // CargoDry.Application injects ICargoDrySettlementPayoutService (from Payment.Abstraction);
        // this registration maps it to the concrete implementation in Payment.Application.
        services.AddScoped<ICargoDrySettlementPayoutService, CargoDrySettlementPayoutService>();

        // ── CargoDry settlement payout lifecycle service (Phase 4D) ──────────
        // Records Approve/Processing/Complete/Fail transitions on PayoutRecordEntity.
        // Does NOT call Iyzico or any payment gateway.
        services.AddScoped<ICargoDrySettlementPayoutLifecycleService, CargoDrySettlementPayoutLifecycleService>();

        // ── CargoDry commission rule lookup service (Phase 5) ─────────────────
        // Allows CargoDry.Application to query Payment commission_rules scoped
        // to ContextType = CargoDry via the cross-module abstraction boundary.
        services.AddScoped<ICargoDryCommissionRuleLookupService, CargoDryCommissionRuleLookupService>();

        // ── CargoDry settlement invoice service (Phase 4C) ────────────────────
        // Bridges CargoDry → Payment module boundary in-process.
        // CargoDry.Application injects ICargoDrySettlementInvoiceService (from Payment.Abstraction);
        // this registration maps it to the concrete implementation in Payment.Application.
        // Creates Draft ProviderSettlementStatement; does NOT issue or create PaymentTransaction.
        services.AddScoped<ICargoDrySettlementInvoiceService, CargoDrySettlementInvoiceService>();

        // ── CargoDry renewal invoice service (Phase 11) ───────────────────────
        // Bridges CargoDry.Application → Payment.Application for kit renewal invoice drafts.
        // Creates Draft CargoDryInvoice; does NOT create a PaymentTransaction.
        services.AddScoped<ICargoDryRenewalInvoiceService, CargoDryRenewalInvoiceService>();

        // ── IAP gateway stubs (Phase 2C: replace with real implementations) ──
        // Apple App Store and Google Play Billing gateway clients are registered
        // as NotImplemented stubs. Phase 2C will swap these for real HTTP clients.
        services.AddScoped<IAppleAppStoreGatewayClient, AppleAppStoreGatewayNotImplementedStub>();
        services.AddScoped<IGooglePlayGatewayClient, GooglePlayGatewayNotImplementedStub>();

        // NOTE: Message consumers (ServiceRequestCompletedConsumer, ServiceRequestCancelledConsumer)
        // are registered in Aizen.Modules.Payment (web host) DI, not here.
        // Consumers must live in the host project — they inline business logic and must NOT
        // dispatch nested CQRS commands via ISender.

        // NOTE: Recurring jobs (PaymentEscrowTimeoutJob, PaymentReminderJob, PayoutProcessingJob,
        // StaleEscrowCleanupJob) are now in Aizen.Modules.Payment/Jobs/ and auto-discovered
        // by AizenApplicationBuilder via host assembly scanning.

        // ── Invoice PDF generation (L11) ────────────────────────────────────
        services.AddScoped<IInvoicePdfRenderer, InvoicePdfRenderer>();
        services.AddScoped<IPaymentInvoicePdfService, PaymentInvoicePdfService>();

        // ── Payout Receipt PDF generation (L12) ─────────────────────────────
        services.AddScoped<IPayoutReceiptPdfRenderer, PayoutReceiptPdfRenderer>();
        services.AddScoped<IPayoutReceiptPdfService, PayoutReceiptPdfService>();

        // CQRS handlers are auto-discovered by AizenApplicationBuilder
        // via assembly scanning when AppType.Scheduler is included in AizenAppInfo.TypeInclude.
        return services;
    }
}
