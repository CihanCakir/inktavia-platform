using System.Reflection;
using Aizen.Modules.Payment.Abstraction.Dto;
using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.Payment.Application.Queries.GetPublicPricingTerms;
using Aizen.Modules.Payment.Domain.Entities.Commission;
using Aizen.Modules.Payment.Domain.Entities.PlatformFee;
using Aizen.Modules.Payment.Repository.Persistence;
using Aizen.Modules.Payment.Repository.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace Aizen.Modules.Payment.Repository.UnitTests;

/// <summary>
/// M1 — the public pricing-terms projection. Proves (a) the commission resolver returns ONLY the Global +
/// Standard-priority + effective-now rule (excludes Emergency / scheduled / expired / non-Global), (b) the platform
/// fee resolver returns ONLY the Global (no category/customer-type) TRY rule, and (c) the handler DTO carries only
/// the published headline figures — no economics internals.
/// </summary>
public sealed class PublicPricingTermsTests
{
    private static PaymentDbContext NewDb()
        => new(new DbContextOptionsBuilder<PaymentDbContext>()
            .UseInMemoryDatabase($"payment-{Guid.NewGuid():N}").Options);

    private static readonly DateTime LongAgo = new(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime Recent  = new(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime Future  = DateTime.UtcNow.AddYears(5);
    private static readonly DateTime Past    = new(2024, 6, 1, 0, 0, 0, DateTimeKind.Utc);

    // ── commission resolver correctness ──────────────────────────────────────────

    [Fact]
    public async Task Commission_resolver_picks_global_standard_effective_now_latest()
    {
        await using var db = NewDb();
        db.CommissionRules.AddRange(
            CommissionRuleEntity.CreateGlobal(0.12m, LongAgo, null, CommissionRulePriority.Standard, null, null),
            CommissionRuleEntity.CreateGlobal(0.15m, Recent, null, CommissionRulePriority.Standard, null, null)); // latest wins
        await db.SaveChangesAsync();

        var rule = await new CommissionRuleRepository(db).GetPublishedStandardGlobalRuleAsync(DateTime.UtcNow);

        rule.Should().NotBeNull();
        rule!.CommissionRate.Should().Be(0.15m);
    }

    [Fact]
    public async Task Commission_resolver_excludes_emergency_scheduled_expired_and_non_global()
    {
        await using var db = NewDb();
        db.CommissionRules.AddRange(
            CommissionRuleEntity.CreateGlobal(0.18m, LongAgo, null, CommissionRulePriority.EMERGENCY, null, null),   // surge — excluded
            CommissionRuleEntity.CreateGlobal(0.20m, Future, null, CommissionRulePriority.Standard, null, null),     // scheduled — excluded
            CommissionRuleEntity.CreateGlobal(0.30m, LongAgo, Past, CommissionRulePriority.Standard, null, null),    // expired — excluded
            CommissionRuleEntity.CreateForCategory("ENGINE", 0.08m, LongAgo, null, CommissionRulePriority.Standard, null, null), // non-global — excluded
            CommissionRuleEntity.CreateProviderOverride(4242, 0.05m, LongAgo, null, CommissionRulePriority.Standard, null, null)); // per-provider — excluded
        await db.SaveChangesAsync();

        var rule = await new CommissionRuleRepository(db).GetPublishedStandardGlobalRuleAsync(DateTime.UtcNow);

        rule.Should().BeNull("none of the seeded rules is a Global + Standard + effective-now rule");
    }

    [Fact]
    public async Task Commission_resolver_excludes_deactivated_global_standard()
    {
        await using var db = NewDb();
        var rule = CommissionRuleEntity.CreateGlobal(0.15m, LongAgo, null, CommissionRulePriority.Standard, null, null);
        rule.Deactivate();
        db.CommissionRules.Add(rule);
        await db.SaveChangesAsync();

        (await new CommissionRuleRepository(db).GetPublishedStandardGlobalRuleAsync(DateTime.UtcNow)).Should().BeNull();
    }

    // ── platform fee resolver correctness ────────────────────────────────────────

    [Fact]
    public async Task PlatformFee_resolver_picks_global_try_and_excludes_scoped_and_other_currency()
    {
        await using var db = NewDb();
        db.PlatformFeeRules.AddRange(
            Fee(categoryCode: null, customerType: null, currency: "TRY"),   // Global — the one we want
            Fee(categoryCode: "ENGINE", customerType: null, currency: "TRY"),  // category-scoped — excluded
            Fee(categoryCode: null, customerType: "GOLD", currency: "TRY"),    // customer-type-scoped — excluded
            Fee(categoryCode: null, customerType: null, currency: "USD"));     // other currency — excluded
        await db.SaveChangesAsync();

        var rule = await new PlatformFeeRuleRepository(db).GetGlobalRuleAsync("TRY", DateTime.UtcNow);

        rule.Should().NotBeNull();
        rule!.CategoryCode.Should().BeNull();
        rule.CustomerType.Should().BeNull();
        rule.CurrencyCode.Should().Be("TRY");
    }

    // ── handler shape + field-stripping ───────────────────────────────────────────

    [Fact]
    public async Task Handler_projects_only_the_published_headline_figures()
    {
        await using var db = NewDb();
        db.CommissionRules.Add(
            CommissionRuleEntity.CreateGlobal(0.15m, Recent, null, CommissionRulePriority.Standard, null, null));
        db.PlatformFeeRules.Add(Fee(null, null, "TRY", from: LongAgo));
        await db.SaveChangesAsync();

        var handler = new GetPublicPricingTermsQueryHandler(
            new CommissionRuleRepository(db), new PlatformFeeRuleRepository(db));
        var dto = await handler.Handle(new GetPublicPricingTermsQuery(), default);

        dto.Should().NotBeNull();
        dto!.Currency.Should().Be("TRY");
        dto.Commission.Audience.Should().Be("provider");
        dto.Commission.StandardRatePercent.Should().Be(15.0m);
        dto.Commission.Note.Should().Contain("may vary");
        dto.CustomerPlatformFee!.Model.Should().Be(PlatformFeeModel.PercentageWithBounds);
        dto.CustomerPlatformFee.RatePercent.Should().Be(2.5m);
        dto.CustomerPlatformFee.MinAmount.Should().Be(99m);
        dto.CustomerPlatformFee.MaxAmount.Should().Be(1500m);
        dto.EffectiveFrom.Should().Be(new DateTimeOffset(Recent), "max EffectiveFrom of the two source rules");
    }

    [Fact]
    public async Task Handler_returns_null_commission_rate_when_no_global_standard_rule()
    {
        await using var db = NewDb();
        db.CommissionRules.Add(
            CommissionRuleEntity.CreateGlobal(0.18m, LongAgo, null, CommissionRulePriority.EMERGENCY, null, null));
        db.PlatformFeeRules.Add(Fee(null, null, "TRY"));
        await db.SaveChangesAsync();

        var handler = new GetPublicPricingTermsQueryHandler(
            new CommissionRuleRepository(db), new PlatformFeeRuleRepository(db));
        var dto = await handler.Handle(new GetPublicPricingTermsQuery(), default);

        dto!.Commission.StandardRatePercent.Should().BeNull("no Global Standard rule — do not fabricate a rate");
    }

    [Fact]
    public void Public_terms_dto_tree_carries_only_headline_fields()
    {
        Props<PublicPricingTermsDto>().Should().BeEquivalentTo(
            "Currency", "Commission", "CustomerPlatformFee", "EffectiveFrom");
        Props<PublicCommissionTermsDto>().Should().BeEquivalentTo(
            "Audience", "Label", "StandardRatePercent", "Note");
        Props<PublicPlatformFeeTermsDto>().Should().BeEquivalentTo(
            "Model", "RatePercent", "MinAmount", "MaxAmount", "Currency");

        // Structural: no economics internal can appear anywhere in the tree.
        var all = Props<PublicPricingTermsDto>()
            .Concat(Props<PublicCommissionTermsDto>())
            .Concat(Props<PublicPlatformFeeTermsDto>())
            .ToArray();
        foreach (var forbidden in new[]
                 {
                     "CostShare", "Tevkifat", "Withhold", "ProfitProtection", "Snapshot", "Net", "Gross",
                     "ProviderProfileId", "ProviderPlanId", "CategoryCode", "CustomerType", "Notes", "RuleCode",
                     "CreateUserId", "ModifyUserId", "Vat", "Benefit", "FixedAmount", "Priority", "Id",
                 })
            all.Should().NotContain(n => n.Contains(forbidden), $"'{forbidden}' must never surface publicly");
    }

    private static PlatformFeeRuleEntity Fee(
        string? categoryCode, string? customerType, string currency, DateTime? from = null)
        => PlatformFeeRuleEntity.Create(
            model:         PlatformFeeModel.PercentageWithBounds,
            rate:          0.025m,
            fixedAmount:   null,
            minAmount:     99m,
            maxAmount:     1500m,
            currencyCode:  currency,
            categoryCode:  categoryCode,
            customerType:  customerType,
            priority:      CommissionRulePriority.Standard,
            effectiveFrom: from ?? LongAgo,
            effectiveTo:   null,
            ruleCode:      null);

    private static string[] Props<T>() => typeof(T)
        .GetProperties(BindingFlags.Public | BindingFlags.Instance)
        .Select(p => p.Name).ToArray();
}
