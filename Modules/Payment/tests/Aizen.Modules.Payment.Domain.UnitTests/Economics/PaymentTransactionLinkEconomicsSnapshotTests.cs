using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.Payment.Abstraction;
using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.Payment.Domain.Entities.Transaction;
using FluentAssertions;

namespace Aizen.Modules.Payment.Domain.UnitTests.Economics;

public sealed class PaymentTransactionLinkEconomicsSnapshotTests
{
    private static PaymentTransactionEntity NewTransaction()
        => PaymentTransactionEntity.Create(
            transactionCode: "TXN-20260728-0001",
            transactionType: TransactionType.ServiceRequestEscrow,
            contextType: TransactionContextType.ServiceRequest,
            contextId: 42, contextSubId: null,
            payerProfileId: 1, recipientProfileId: 2,
            grossAmount: 1120.00m, commissionAmount: 150.00m, commissionRateSnapshot: 0.1500m,
            vatOnCommission: 0m, netPayoutAmount: 850.00m, discountAmount: 0m,
            currencyCode: "TRY", gatewayProvider: "iyzico",
            idempotencyKey: "idem-1", escrowRequired: true);

    [Fact]
    public void LinkEconomicsSnapshot_Sets_The_Id_When_Unset()
    {
        var tx = NewTransaction();
        tx.EconomicsSnapshotId.Should().BeNull();

        tx.LinkEconomicsSnapshot(555);

        tx.EconomicsSnapshotId.Should().Be(555);
    }

    [Fact]
    public void LinkEconomicsSnapshot_Is_Idempotent_For_Same_Id()
    {
        var tx = NewTransaction();
        tx.LinkEconomicsSnapshot(555);

        Action again = () => tx.LinkEconomicsSnapshot(555);

        again.Should().NotThrow();
        tx.EconomicsSnapshotId.Should().Be(555);
    }

    [Fact]
    public void LinkEconomicsSnapshot_Throws_When_Relinking_To_A_Different_Id()
    {
        var tx = NewTransaction();
        tx.LinkEconomicsSnapshot(555);

        Action relink = () => tx.LinkEconomicsSnapshot(999);

        relink.Should().Throw<AizenBusinessException>();
        tx.EconomicsSnapshotId.Should().Be(555); // unchanged
    }
}
