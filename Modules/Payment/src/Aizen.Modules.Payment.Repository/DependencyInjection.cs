using Aizen.Modules.Payment.Domain.Interface.Repository;
using Aizen.Modules.Payment.Repository.Persistence;
using Aizen.Modules.Payment.Repository.Repositories;
using Aizen.Modules.Payment.Repository.Seed;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Aizen.Modules.Payment.Repository;

public static class DependencyInjection
{
    public static IServiceCollection AddPaymentRepository(
        this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IPaymentTransactionRepository,    PaymentTransactionRepository>();
        services.AddScoped<ICommissionRuleRepository,        CommissionRuleRepository>();
        services.AddScoped<IProviderPlanRepository,          ProviderPlanRepository>();
        services.AddScoped<IParticipantPlanRepository,       ParticipantPlanRepository>();
        services.AddScoped<IPayoutRecordRepository,          PayoutRecordRepository>();
        services.AddScoped<IProviderPaymentProfileRepository, ProviderPaymentProfileRepository>();
        // ── Economics ledger (BE-P1) ──────────────────────────────────────────
        services.AddScoped<IPaymentEconomicsSnapshotRepository, PaymentEconomicsSnapshotRepository>();
        // ── Platform fee rules (BE-P3) ────────────────────────────────────────
        services.AddScoped<IPlatformFeeRuleRepository,          PlatformFeeRuleRepository>();
        // ── Provider plan prices (BE-P4) ──────────────────────────────────────
        services.AddScoped<IProviderPlanPriceRepository,        ProviderPlanPriceRepository>();
        // ── Profit protection (BE-P5) ─────────────────────────────────────────
        services.AddScoped<IProfitProtectionPolicyRepository,         ProfitProtectionPolicyRepository>();
        services.AddScoped<IProfitProtectionEvaluationLogRepository,  ProfitProtectionEvaluationLogRepository>();
        services.AddScoped<ILineProfitProtectionEvaluationLogRepository, LineProfitProtectionEvaluationLogRepository>();
        // ── Customer discount + benefit budget (BE-P6) ────────────────────────
        services.AddScoped<ICustomerDiscountRuleRepository,          CustomerDiscountRuleRepository>();
        services.AddScoped<ICustomerBenefitBudgetRepository,         CustomerBenefitBudgetRepository>();
        services.AddScoped<ICustomerBenefitBudgetPolicyRepository,   CustomerBenefitBudgetPolicyRepository>();
        // ── Provider commission benefit (BE-P7) ───────────────────────────────
        services.AddScoped<IProviderCommissionBenefitRuleRepository,        ProviderCommissionBenefitRuleRepository>();
        services.AddScoped<IProviderCommissionBenefitEntitlementRepository, ProviderCommissionBenefitEntitlementRepository>();
        // ── Refund allocation + provider balance + chargeback (BE-P10) ────────
        services.AddScoped<IRefundAllocationPolicyRepository, RefundAllocationPolicyRepository>();
        services.AddScoped<IRefundAllocationRepository,       RefundAllocationRepository>();
        services.AddScoped<IProviderBalanceRepository,        ProviderBalanceRepository>();
        services.AddScoped<IChargebackRecordRepository,       ChargebackRecordRepository>();
        // ── Premium product / price / purchase / entitlement (BE-P11) ─────────
        services.AddScoped<IPremiumProductRepository,        PremiumProductRepository>();
        services.AddScoped<IPremiumProductPriceRepository,   PremiumProductPriceRepository>();
        services.AddScoped<IPremiumPurchaseRepository,       PremiumPurchaseRepository>();
        services.AddScoped<IPremiumEntitlementRepository,    PremiumEntitlementRepository>();
        // ── Part commercial terms (BE-S5) ─────────────────────────────────────
        services.AddScoped<IPartCommercialTermRepository,    PartCommercialTermRepository>();
        // ── Financial reporting ledger (BE-P12) ───────────────────────────────
        services.AddScoped<IFinancialLedgerRepository,       FinancialLedgerRepository>();
        // ── Invoice subsystem ─────────────────────────────────────────────────
        services.AddScoped<IInvoiceRepository,               InvoiceRepository>();
        services.AddScoped<IInvoiceNumberSequenceRepository, InvoiceNumberSequenceRepository>();
        services.AddScoped<RefundAllocationPolicySeed>();
        services.AddScoped<PremiumProductSeed>();
        services.AddScoped<PaymentPlanSeed>();
        services.AddScoped<PlatformFeeRuleSeed>();
        services.AddScoped<ProviderPlanPriceSeed>();
        services.AddScoped<ProfitProtectionPolicySeed>();
        services.AddScoped<CustomerDiscountBenefitSeed>();
        services.AddScoped<ProviderCommissionBenefitSeed>();
        services.AddScoped<PartCommercialTermSeed>();
        // ── Mock / demo seeds (dev + local only) ─────────────────────────────
        services.AddScoped<PaymentTransactionMockSeed>();
        services.AddScoped<PayoutRecordMockSeed>();
        services.AddScoped<SubscriptionMockSeed>();
        services.AddScoped<TransactionRefundMockSeed>();
        services.AddScoped<CommissionRuleMockSeed>();
        services.AddScoped<InvoiceMockSeed>();
        services.AddScoped<Provider2PositiveBranchMockSeed>();
        services.AddScoped<SubMerchantOnboardingMockSeed>();

        return services;
    }

    public static async Task SeedPaymentAsync(this IHost host, CancellationToken ct = default)
    {
        using var scope = host.Services.CreateScope();

        var db      = scope.ServiceProvider.GetRequiredService<PaymentDbContext>();
        var pending = await db.Database.GetPendingMigrationsAsync(ct);
        if (pending.Any()) await db.Database.MigrateAsync(ct);

        // Phase 1: plans + commission rules (non-mock, always runs)
        var planSeeder = scope.ServiceProvider.GetRequiredService<PaymentPlanSeed>();
        await planSeeder.SeedAsync(ct);

        // Phase 1b: platform fee rules (non-mock, always runs, idempotent — BE-P3)
        var platformFeeSeeder = scope.ServiceProvider.GetRequiredService<PlatformFeeRuleSeed>();
        await platformFeeSeeder.SeedAsync(ct);

        // Phase 1c: provider plan prices (non-mock, always runs, idempotent — BE-P4). Runs after PaymentPlanSeed
        // so provider plan PKs exist.
        var planPriceSeeder = scope.ServiceProvider.GetRequiredService<ProviderPlanPriceSeed>();
        await planPriceSeeder.SeedAsync(ct);

        // Phase 1d: profit protection policy (non-mock, always runs, idempotent — BE-P5)
        var profitProtectionSeeder = scope.ServiceProvider.GetRequiredService<ProfitProtectionPolicySeed>();
        await profitProtectionSeeder.SeedAsync(ct);

        // Phase 1e: customer discount reconciliation + benefit budget policies (idempotent — BE-P6).
        // Runs after PaymentPlanSeed so participant plan PKs exist.
        var customerDiscountSeeder = scope.ServiceProvider.GetRequiredService<CustomerDiscountBenefitSeed>();
        await customerDiscountSeeder.SeedAsync(ct);

        // Phase 1f: provider commission benefit — disabled example only (benefits OFF by default — BE-P7)
        var commissionBenefitSeeder = scope.ServiceProvider.GetRequiredService<ProviderCommissionBenefitSeed>();
        await commissionBenefitSeeder.SeedAsync(ct);

        // Phase 1f2: part commercial terms (non-mock, always runs, idempotent — BE-S5). Defines/resolves only (S9 applies).
        var partTermSeeder = scope.ServiceProvider.GetRequiredService<PartCommercialTermSeed>();
        await partTermSeeder.SeedAsync(ct);

        // Phase 1g: refund-allocation policy (non-mock, always runs, idempotent — BE-P10)
        var refundAllocationSeeder = scope.ServiceProvider.GetRequiredService<RefundAllocationPolicySeed>();
        await refundAllocationSeeder.SeedAsync(ct);

        // Phase 1h: premium product OFFER_BOOST_7D + launch price (non-mock, always runs, idempotent — BE-P11)
        var premiumSeeder = scope.ServiceProvider.GetRequiredService<PremiumProductSeed>();
        await premiumSeeder.SeedAsync(ct);

        // Phase 2: mock transaction data (all idempotent — safe to call in dev/local)
        var txSeeder  = scope.ServiceProvider.GetRequiredService<PaymentTransactionMockSeed>();
        await txSeeder.SeedAsync(ct);

        // Phase 3: payouts — must run after transactions (FK)
        var payoutSeeder = scope.ServiceProvider.GetRequiredService<PayoutRecordMockSeed>();
        await payoutSeeder.SeedAsync(ct);

        // Phase 4: subscriptions — must run after plans (FK)
        var subSeeder = scope.ServiceProvider.GetRequiredService<SubscriptionMockSeed>();
        await subSeeder.SeedAsync(ct);

        // Phase 5: refunds — must run after transactions (FK + domain apply)
        var refundSeeder = scope.ServiceProvider.GetRequiredService<TransactionRefundMockSeed>();
        await refundSeeder.SeedAsync(ct);

        // Phase 6: commission rule mock data (admin panel detail page — idempotent by RuleCode)
        var commRuleSeeder = scope.ServiceProvider.GetRequiredService<CommissionRuleMockSeed>();
        await commRuleSeeder.SeedAsync(ct);

        // Phase 7: invoice mock data (provider2 settlement statement)
        var invoiceSeeder = scope.ServiceProvider.GetRequiredService<InvoiceMockSeed>();
        await invoiceSeeder.SeedAsync(ct);

        // Phase 8: Provider2 Wave B positive branches — economics snapshot (Part A) + disputed/refund + over-limit
        // negative balance (Part C). Must run after transactions (own tx codes) and refunds. Idempotent, demo-only.
        var waveBSeeder = scope.ServiceProvider.GetRequiredService<Provider2PositiveBranchMockSeed>();
        await waveBSeeder.SeedAsync(ct);

        // Phase 9: sub-merchant onboarding demo profiles for the BE-I1 admin KYC queue (idempotent, demo-only).
        var subMerchantSeeder = scope.ServiceProvider.GetRequiredService<SubMerchantOnboardingMockSeed>();
        await subMerchantSeeder.SeedAsync(ct);
    }
}
