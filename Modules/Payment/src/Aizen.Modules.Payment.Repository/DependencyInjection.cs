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

        return services;
    }

    public static async Task SeedPaymentAsync(this IHost host, CancellationToken ct = default)
    {
        using var scope = host.Services.CreateScope();

        var db      = scope.ServiceProvider.GetRequiredService<PaymentDbContext>();
        var pending = await db.Database.GetPendingMigrationsAsync(ct);
        if (pending.Any()) await db.Database.MigrateAsync(ct);

        var seeder = scope.ServiceProvider.GetRequiredService<PaymentPlanSeed>();
        await seeder.SeedAsync(ct);
    }
}
