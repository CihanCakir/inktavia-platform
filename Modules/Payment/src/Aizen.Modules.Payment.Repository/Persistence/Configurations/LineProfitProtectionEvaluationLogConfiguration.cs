using Aizen.Modules.Payment.Domain.Entities.ProfitProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aizen.Modules.Payment.Repository.Persistence.Configurations;

/// <summary>
/// BE-S9 (§20.12) — EF mapping for the insert-only line-level profit-protection evaluation log. Written for non-Approved
/// (Rejected / ConfigurationError) line-level decisions; enum int; index on (CurrencyCode, DecisionState, EvaluatedAtUtc)
/// and on (ServiceRequestId, OfferId) for the acceptance lookup.
/// </summary>
public sealed class LineProfitProtectionEvaluationLogConfiguration
    : IEntityTypeConfiguration<LineProfitProtectionEvaluationLogEntity>
{
    public void Configure(EntityTypeBuilder<LineProfitProtectionEvaluationLogEntity> b)
    {
        b.ToTable("line_profit_protection_evaluation_logs");
        b.HasKey(x => x.Id);

        b.Property(x => x.ServiceRequestId).IsRequired();
        b.Property(x => x.OfferId).IsRequired();
        b.Property(x => x.PolicyId);
        b.Property(x => x.CurrencyCode).HasMaxLength(10).IsRequired();
        b.Property(x => x.DecisionState).HasConversion<int>().IsRequired();
        b.Property(x => x.EvaluatedAtUtc).IsRequired();
        b.Property(x => x.LineCount).IsRequired();
        b.Property(x => x.FailedLineCount).IsRequired();
        b.Property(x => x.PrimaryBreachCode);
        b.Property(x => x.FailedLineRefs).HasMaxLength(1000);
        b.Property(x => x.Reason).HasMaxLength(2000);

        b.HasIndex(x => new { x.CurrencyCode, x.DecisionState, x.EvaluatedAtUtc });
        b.HasIndex(x => new { x.ServiceRequestId, x.OfferId });
    }
}
