using System.Reflection;
using Aizen.Core.CQRS.GenericHandler;
using Aizen.Core.CQRS.GenericMessage;
using Aizen.Core.Domain;
using Aizen.Core.UnitOfWork.Abstraction;
using Aizen.Modules.Payment.Domain.Entities.Commission;
using Aizen.Modules.Payment.Domain.Entities.Economics;
using Aizen.Modules.Payment.Domain.Entities.PartCommercialTerm;
using Aizen.Modules.Payment.Domain.Entities.Premium;
using Aizen.Modules.Payment.Domain.Entities.ProfitProtection;
using Aizen.Modules.Payment.Domain.Entities.RefundAllocation;
using Aizen.Modules.Payment.Domain.Entities.Reporting;
using Aizen.Modules.Payment.Domain.Entities.Transaction;
using FluentAssertions;

namespace Aizen.Modules.Payment.Domain.UnitTests.MessagebusHardening;

/// <summary>
/// HARDENING_MESSAGEBUS_GENERIC_CONSUMER_OPTOUT — proves the generic-sync bypass is
/// closed for domain-authored immutable financial entities:
///   (1) the opt-out policy flags the marked financial set (and only it),
///   (2) the production registration filter excludes them (→ no AizenGenericConsumer),
///   (3) the generic Insert/Update/Delete handlers refuse them (fail loud, no write),
///   (4) an unmarked entity is unaffected — still policy-allowed and still registered.
/// </summary>
public class GenericConsumerOptOutTests
{
    // The full financial set marked [NoMessagebusSync]. Kept explicit so the test fails
    // loudly if a marker is ever removed from one of these entities.
    public static readonly Type[] MarkedFinancialEntities =
    {
        typeof(PaymentEconomicsSnapshotEntity),
        typeof(OfferLineEconomicsSnapshotEntity),
        typeof(CommissionAllocationSnapshotEntity),
        typeof(DiscountAllocationSnapshotEntity),
        typeof(TravelPricingSnapshotEntity),
        typeof(OfferLineAttributeSnapshotEntity),
        typeof(FinancialLedgerEntryEntity),
        typeof(ProfitProtectionEvaluationLogEntity),
        typeof(LineProfitProtectionEvaluationLogEntity),
        typeof(RefundAllocationEntity),
        typeof(ChargebackRecordEntity),
        typeof(ProviderBalanceEntity),
        typeof(ProviderBalanceMovementEntity),
        typeof(PremiumPurchaseEntity),
        typeof(PremiumEntitlementEntity),
        typeof(PartCommercialTermEntity),
        typeof(TransactionRefundRecord),
    };

    public static IEnumerable<object[]> MarkedFinancialEntityCases()
        => MarkedFinancialEntities.Select(t => new object[] { t });

    // ---------------------------------------------------------------------------------
    // (1) The opt-out policy flags exactly the marked financial set.
    // ---------------------------------------------------------------------------------

    [Theory]
    [MemberData(nameof(MarkedFinancialEntityCases))]
    public void Policy_flags_every_marked_financial_entity(Type entityType)
    {
        MessagebusSyncPolicy.IsGenericSyncBlocked(entityType).Should().BeTrue(
            $"{entityType.Name} is a domain-authored immutable financial entity and must be opted out of generic sync");
    }

    [Fact]
    public void Policy_does_not_flag_an_ordinary_unmarked_entity()
    {
        // CommissionRuleEntity is an ordinary mutable rule entity — it must keep its generic consumer.
        MessagebusSyncPolicy.IsGenericSyncBlocked(typeof(CommissionRuleEntity)).Should().BeFalse();
    }

    // ---------------------------------------------------------------------------------
    // (2) The production registration filter (mirrors BuilderExtensions' AizenEntity loop)
    //     excludes marked entities and keeps unmarked ones — i.e. no AizenGenericConsumer
    //     is wired for the financial set, but every other entity is still registered.
    // ---------------------------------------------------------------------------------

    private static IReadOnlyCollection<Type> RegistrableGenericConsumerEntities(Assembly domainAssembly)
        => domainAssembly.GetTypes()
            // identical predicate to BuilderExtensions.AddAizenMessagebus:
            .Where(type => type is { IsClass: true, IsAbstract: false } &&
                           typeof(AizenEntity).IsAssignableFrom(type))
            .Where(type => !MessagebusSyncPolicy.IsGenericSyncBlocked(type))
            .ToArray();

    [Fact]
    public void Registration_filter_excludes_the_marked_financial_entities()
    {
        var registrable = RegistrableGenericConsumerEntities(typeof(PaymentEconomicsSnapshotEntity).Assembly);

        registrable.Should().NotContain(MarkedFinancialEntities,
            "no AizenGenericConsumer<T> may be registered for a [NoMessagebusSync] financial entity");
    }

    [Fact]
    public void Registration_filter_still_includes_an_unmarked_entity()
    {
        var registrable = RegistrableGenericConsumerEntities(typeof(CommissionRuleEntity).Assembly);

        registrable.Should().Contain(typeof(CommissionRuleEntity),
            "ordinary entities keep their generic consumer — the change is behaviour-preserving for them");
    }

    [Fact]
    public void Hazard_closed_specifically_for_PaymentEconomicsSnapshotEntity()
    {
        var registrable = RegistrableGenericConsumerEntities(typeof(PaymentEconomicsSnapshotEntity).Assembly);

        registrable.Should().NotContain(typeof(PaymentEconomicsSnapshotEntity));
        MessagebusSyncPolicy.IsGenericSyncBlocked(typeof(PaymentEconomicsSnapshotEntity)).Should().BeTrue();
    }

    // ---------------------------------------------------------------------------------
    // (3) Defense-in-depth: even if a generic command reaches a handler, it is refused
    //     BEFORE any repository is touched (fail loud, no write).
    //     UnitOfWork is deliberately empty → if the guard did not fire first, the handler
    //     would NullReference on GetRepository; asserting InvalidOperationException proves
    //     the guard short-circuited before any persistence.
    // ---------------------------------------------------------------------------------

    private static readonly IEnumerable<IAizenUnitOfWork> NoUnitOfWorks = Array.Empty<IAizenUnitOfWork>();

    [Fact]
    public async Task Generic_insert_is_refused_for_a_marked_entity()
    {
        var handler = new AizenInsertEntityCommandHandler<PaymentEconomicsSnapshotEntity>(NoUnitOfWorks);
        var command = new AizenInsertEntityCommand<PaymentEconomicsSnapshotEntity> { Entity = null! };

        var act = () => handler.Handle(command, CancellationToken.None);

        (await act.Should().ThrowAsync<InvalidOperationException>())
            .WithMessage("*NoMessagebusSync*");
    }

    [Fact]
    public async Task Generic_update_is_refused_for_a_marked_entity()
    {
        var handler = new AizenUpdateEntityCommandHandler<PaymentEconomicsSnapshotEntity>(NoUnitOfWorks);
        var command = new AizenUpdateEntityCommand<PaymentEconomicsSnapshotEntity> { Entity = null! };

        var act = () => handler.Handle(command, CancellationToken.None);

        (await act.Should().ThrowAsync<InvalidOperationException>())
            .WithMessage("*NoMessagebusSync*");
    }

    [Fact]
    public async Task Generic_delete_is_refused_for_a_marked_entity()
    {
        var handler = new AizenDeleteEntityCommandHandler<PaymentEconomicsSnapshotEntity>(NoUnitOfWorks);
        var command = new AizenDeleteEntityCommand<PaymentEconomicsSnapshotEntity> { Entity = null! };

        var act = () => handler.Handle(command, CancellationToken.None);

        (await act.Should().ThrowAsync<InvalidOperationException>())
            .WithMessage("*NoMessagebusSync*");
    }
}
