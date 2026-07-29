using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.Payment.Application.Commands.SubscribeProviderPlan;
using Aizen.Modules.Payment.Domain.Entities.Plan;
using Aizen.Modules.Payment.Repository.Persistence;
using Aizen.Modules.Payment.Repository.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace Aizen.Modules.Payment.Repository.UnitTests;

/// <summary>
/// BE-P4: subscribe must snapshot the RESOLVED ProviderPlanPrice, not the (untrusted) request amount and not
/// ProviderPlan.MonthlyPriceTRY. Integration test over the real handler + repositories on an in-memory DbContext.
/// </summary>
public sealed class SubscribeProviderPlanSnapshotTests
{
    private static PaymentDbContext NewDb()
        => new(new DbContextOptionsBuilder<PaymentDbContext>()
            .UseInMemoryDatabase($"sub-{Guid.NewGuid():N}").Options);

    [Fact]
    public async Task Subscribe_Snapshots_Resolved_Price_Not_Request_Amount()
    {
        await using var db = NewDb();

        // Plan whose display price (MonthlyPriceTRY) is deliberately different from the authoritative price row.
        var plan = ProviderPlanEntity.Create("STANDARD", "Standard", null,
            monthlyPriceTRY: 999m, null, null, null, null, false, true, 2);
        db.ProviderPlans.Add(plan);
        await db.SaveChangesAsync();

        // Authoritative price row covering "now" = 499 (now-relative window so it's clock-robust).
        var now = DateTime.UtcNow;
        db.ProviderPlanPrices.Add(ProviderPlanPriceEntity.Create(
            plan.Id, ProviderPlanPriceType.Launch, BillingPeriod.Monthly, 499m, "TRY",
            now.AddDays(-30), now.AddDays(30), "P-NOW"));
        await db.SaveChangesAsync();

        var handler = new SubscribeProviderPlanCommandHandler(
            unitOfWork:    null!,                                   // unused by the handler
            plans:         new ProviderPlanRepository(db),
            planPrices:    new ProviderPlanPriceRepository(db),
            ledgerPosting: new Aizen.Modules.Payment.Application.Services.FinancialLedgerPostingService(
                               new FinancialLedgerRepository(db),
                               NullLogger<Aizen.Modules.Payment.Application.Services.FinancialLedgerPostingService>.Instance),
            logger:        NullLogger<SubscribeProviderPlanCommandHandler>.Instance);

        var result = await handler.Handle(new SubscribeProviderPlanCommand
        {
            ProviderProfileId = 77,
            ProviderPlanId    = plan.Id,
            PaidAmount        = 12345m,          // untrusted / wrong — must be ignored
            CurrencyCode      = "TRY",
            PeriodStart       = now,
            PeriodEnd         = now.AddMonths(1),
            AutoRenew         = true,
        }, default);

        result!.PaidAmount.Should().Be(499m, "the resolved ProviderPlanPrice is authoritative, not the request or MonthlyPriceTRY");
    }
}
