using Aizen.Core.EFCore;
using Aizen.Modules.Payment.Domain.Entities.Commission;
using Aizen.Modules.Payment.Domain.Entities.CommissionBenefit;
using Aizen.Modules.Payment.Domain.Entities.CustomerBenefit;
using Aizen.Modules.Payment.Domain.Entities.RefundAllocation;
using Aizen.Modules.Payment.Domain.Entities.Premium;
using Aizen.Modules.Payment.Domain.Entities.Reporting;
using Aizen.Modules.Payment.Domain.Entities.CustomerDiscount;
using Aizen.Modules.Payment.Domain.Entities.Economics;
using Aizen.Modules.Payment.Domain.Entities.Invoice;
using Aizen.Modules.Payment.Domain.Entities.PaymentProfile;
using Aizen.Modules.Payment.Domain.Entities.Payout;
using Aizen.Modules.Payment.Domain.Entities.PlatformFee;
using Aizen.Modules.Payment.Domain.Entities.Plan;
using Aizen.Modules.Payment.Domain.Entities.ProfitProtection;
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

    // ── Economics ledger (BE-P1) ──────────────────────────────────────────────
    public DbSet<PaymentEconomicsSnapshotEntity>  PaymentEconomicsSnapshots => Set<PaymentEconomicsSnapshotEntity>();

    // ── Line economics snapshots (BE-S8, §20.15) — insert-only children ────────
    public DbSet<OfferLineEconomicsSnapshotEntity> OfferLineEconomicsSnapshots => Set<OfferLineEconomicsSnapshotEntity>();
    public DbSet<CommissionAllocationSnapshotEntity> CommissionAllocationSnapshots => Set<CommissionAllocationSnapshotEntity>();
    public DbSet<DiscountAllocationSnapshotEntity> DiscountAllocationSnapshots => Set<DiscountAllocationSnapshotEntity>();
    // S2d — per-attribute line snapshot children (§20.6)
    public DbSet<OfferLineAttributeSnapshotEntity> OfferLineAttributeSnapshots => Set<OfferLineAttributeSnapshotEntity>();
    // S4b — travel/mobilization snapshot children of the aggregate (§20.8)
    public DbSet<TravelPricingSnapshotEntity> TravelPricingSnapshots => Set<TravelPricingSnapshotEntity>();

    // ── Platform fee rules (BE-P3) ────────────────────────────────────────────
    public DbSet<PlatformFeeRuleEntity>           PlatformFeeRules      => Set<PlatformFeeRuleEntity>();

    // ── Provider plan prices (BE-P4) ──────────────────────────────────────────
    public DbSet<ProviderPlanPriceEntity>         ProviderPlanPrices    => Set<ProviderPlanPriceEntity>();

    // ── Profit protection (BE-P5) ─────────────────────────────────────────────
    public DbSet<ProfitProtectionPolicyEntity>        ProfitProtectionPolicies       => Set<ProfitProtectionPolicyEntity>();
    public DbSet<ProfitProtectionEvaluationLogEntity> ProfitProtectionEvaluationLogs => Set<ProfitProtectionEvaluationLogEntity>();

    // ── Customer discount + benefit budget (BE-P6) ────────────────────────────
    public DbSet<CustomerDiscountRuleEntity>          CustomerDiscountRules          => Set<CustomerDiscountRuleEntity>();
    public DbSet<CustomerBenefitBudgetEntity>         CustomerBenefitBudgets         => Set<CustomerBenefitBudgetEntity>();
    public DbSet<CustomerBenefitReservationEntity>    CustomerBenefitReservations    => Set<CustomerBenefitReservationEntity>();
    public DbSet<CustomerBenefitBudgetPolicyEntity>   CustomerBenefitBudgetPolicies  => Set<CustomerBenefitBudgetPolicyEntity>();

    // ── Provider commission benefit (BE-P7) ───────────────────────────────────
    public DbSet<ProviderCommissionBenefitRuleEntity>        ProviderCommissionBenefitRules        => Set<ProviderCommissionBenefitRuleEntity>();
    public DbSet<ProviderCommissionBenefitEntitlementEntity> ProviderCommissionBenefitEntitlements => Set<ProviderCommissionBenefitEntitlementEntity>();
    public DbSet<ProviderCommissionBenefitUsageEntity>       ProviderCommissionBenefitUsages       => Set<ProviderCommissionBenefitUsageEntity>();

    // ── Refund allocation + provider balance + chargeback (BE-P10) ─────────────
    public DbSet<RefundAllocationPolicyEntity>      RefundAllocationPolicies      => Set<RefundAllocationPolicyEntity>();
    public DbSet<RefundAllocationPolicyRuleEntity>  RefundAllocationPolicyRules   => Set<RefundAllocationPolicyRuleEntity>();
    public DbSet<RefundAllocationEntity>            RefundAllocations             => Set<RefundAllocationEntity>();
    public DbSet<ProviderBalanceEntity>             ProviderBalances              => Set<ProviderBalanceEntity>();
    public DbSet<ProviderBalanceMovementEntity>     ProviderBalanceMovements      => Set<ProviderBalanceMovementEntity>();
    public DbSet<ChargebackRecordEntity>            ChargebackRecords             => Set<ChargebackRecordEntity>();

    // ── Financial reporting ledger (BE-P12) ────────────────────────────────────
    public DbSet<FinancialLedgerEntryEntity>        FinancialLedgerEntries        => Set<FinancialLedgerEntryEntity>();

    // ── Premium product / price / purchase / entitlement (BE-P11) ──────────────
    public DbSet<PremiumProductEntity>              PremiumProducts               => Set<PremiumProductEntity>();
    public DbSet<PremiumProductPriceEntity>         PremiumProductPrices          => Set<PremiumProductPriceEntity>();
    public DbSet<PremiumPurchaseEntity>             PremiumPurchases              => Set<PremiumPurchaseEntity>();
    public DbSet<PremiumEntitlementEntity>          PremiumEntitlements           => Set<PremiumEntitlementEntity>();

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
