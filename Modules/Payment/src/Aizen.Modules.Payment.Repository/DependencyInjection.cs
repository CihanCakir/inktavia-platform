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
        // ── Invoice subsystem ─────────────────────────────────────────────────
        services.AddScoped<IInvoiceRepository,               InvoiceRepository>();
        services.AddScoped<IInvoiceNumberSequenceRepository, InvoiceNumberSequenceRepository>();
        services.AddScoped<PaymentPlanSeed>();
        // ── Mock / demo seeds (dev + local only) ─────────────────────────────
        services.AddScoped<PaymentTransactionMockSeed>();
        services.AddScoped<PayoutRecordMockSeed>();
        services.AddScoped<SubscriptionMockSeed>();
        services.AddScoped<TransactionRefundMockSeed>();
        services.AddScoped<CommissionRuleMockSeed>();

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
    }
}
