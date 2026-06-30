using Aizen.Core.EFCore;
using Aizen.Modules.Payment.Domain.Entities.Commission;
using Aizen.Modules.Payment.Domain.Entities.Invoice;
using Aizen.Modules.Payment.Domain.Entities.PaymentProfile;
using Aizen.Modules.Payment.Domain.Entities.Payout;
using Aizen.Modules.Payment.Domain.Entities.Plan;
using Aizen.Modules.Payment.Domain.Entities.Subscription;
using Aizen.Modules.Payment.Domain.Entities.Transaction;
using Microsoft.EntityFrameworkCore;

namespace Aizen.Modules.Payment.Repository.Persistence;

[DocumentationInfo("Payment EF DbContext",
    "EF Core context for the Payment module PostgreSQL schema (payment.*).")]
public sealed class PaymentDbContext : AizenDbContext
{
    public PaymentDbContext(DbContextOptions<PaymentDbContext> options) : base(options) { }

    public DbSet<PaymentTransactionEntity>        Transactions          => Set<PaymentTransactionEntity>();
    public DbSet<TransactionRefundRecord>         TransactionRefunds    => Set<TransactionRefundRecord>();
    public DbSet<CommissionRuleEntity>            CommissionRules       => Set<CommissionRuleEntity>();
    public DbSet<ProviderPlanEntity>              ProviderPlans         => Set<ProviderPlanEntity>();
    public DbSet<ParticipantPlanEntity>           ParticipantPlans      => Set<ParticipantPlanEntity>();
    public DbSet<ProviderPlanSubscriptionEntity>  ProviderSubscriptions => Set<ProviderPlanSubscriptionEntity>();
    public DbSet<ParticipantPlanSubscriptionEntity> ParticipantSubscriptions => Set<ParticipantPlanSubscriptionEntity>();
    public DbSet<PayoutRecordEntity>              PayoutRecords         => Set<PayoutRecordEntity>();
    public DbSet<ProviderPaymentProfileEntity>    PaymentProfiles       => Set<ProviderPaymentProfileEntity>();

    // ── Invoice subsystem ─────────────────────────────────────────────────────
    public DbSet<InvoiceHeaderEntity>                InvoiceHeaders           => Set<InvoiceHeaderEntity>();
    public DbSet<InvoiceLineEntity>                  InvoiceLines             => Set<InvoiceLineEntity>();
    public DbSet<InvoiceTaxBreakdownEntity>          InvoiceTaxBreakdowns     => Set<InvoiceTaxBreakdownEntity>();
    public DbSet<InvoiceStatusHistoryEntity>         InvoiceStatusHistories   => Set<InvoiceStatusHistoryEntity>();
    public DbSet<InvoiceNumberSequenceEntity>        InvoiceNumberSequences   => Set<InvoiceNumberSequenceEntity>();
    public DbSet<InvoiceExternalIntegrationEntity>   InvoiceExternalIntegrations => Set<InvoiceExternalIntegrationEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema("payment");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PaymentDbContext).Assembly);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        NormalizeDateTimeProperties();
        return base.SaveChangesAsync(cancellationToken);
    }

    public override int SaveChanges()
    {
        NormalizeDateTimeProperties();
        return base.SaveChanges();
    }

    private void NormalizeDateTimeProperties()
    {
        foreach (var entry in ChangeTracker.Entries())
        {
            if (entry.State is not (EntityState.Added or EntityState.Modified)) continue;
            foreach (var property in entry.Properties)
            {
                if (property.CurrentValue is DateTime dt && dt.Kind == DateTimeKind.Local)
                    property.CurrentValue = dt.ToUniversalTime();
            }
        }
    }
}
