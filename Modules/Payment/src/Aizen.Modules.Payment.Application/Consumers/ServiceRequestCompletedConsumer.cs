using Aizen.Core.Messagebus.Abstraction.Consumers;
using Aizen.Core.Messagebus.Abstraction.Messages;
using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.Payment.Application.Commands.ReleasePaymentEscrow;
using Aizen.Modules.Payment.Domain.Interface.Repository;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Aizen.Modules.Payment.Application.Consumers;

/// <summary>
/// Listens for ServiceRequestCompletedMessage from the ServiceRequest module.
/// When SR completion is approved, this consumer triggers ReleasePaymentEscrowCommand
/// to transfer escrowed funds to the provider sub-merchant via Iyzico marketplace approval.
///
/// Idempotency: if the transaction is already Released, the command handler returns without error.
/// </summary>
public sealed class ServiceRequestCompletedConsumer
    : AizenBaseMessageConsumer<ServiceRequestCompletedMessage>
{
    private readonly ISender                                              _sender;
    private readonly IPaymentTransactionRepository                        _transactions;
    private readonly ILogger<ServiceRequestCompletedConsumer>             _logger;

    public ServiceRequestCompletedConsumer(IServiceProvider sp) : base(sp)
    {
        _sender       = sp.GetRequiredService<ISender>();
        _transactions = sp.GetRequiredService<IPaymentTransactionRepository>();
        _logger       = sp.GetRequiredService<ILogger<ServiceRequestCompletedConsumer>>();
    }

    public override async Task<bool> ExecutePrepareMessage(
        ServiceRequestCompletedMessage message, CancellationToken ct)
    {
        // Verify a captured transaction exists for this SR before committing
        var tx = await _transactions.GetByContextAsync(
            TransactionContextType.ServiceRequest, message.ServiceRequestId, ct);

        if (tx is null)
        {
            _logger.LogWarning(
                "ServiceRequestCompletedConsumer: no transaction found for SR {SRId}. Skipping escrow release.",
                message.ServiceRequestId);
            return false; // Rollback — nothing to do
        }

        if (tx.Status == Abstraction.Enum.PaymentTransactionStatus.Released)
        {
            _logger.LogInformation(
                "ServiceRequestCompletedConsumer: transaction {TxId} already released. Idempotent skip.",
                tx.Id);
            return false;
        }

        return true;
    }

    public override async Task ExecuteCommitMessage(
        ServiceRequestCompletedMessage message, CancellationToken ct)
    {
        var tx = await _transactions.GetByContextAsync(
            TransactionContextType.ServiceRequest, message.ServiceRequestId, ct);

        if (tx is null) return;

        _logger.LogInformation(
            "ServiceRequestCompletedConsumer: releasing escrow for SR {SRId} → Transaction {TxId}",
            message.ServiceRequestId, tx.Id);

        await _sender.Send(new ReleasePaymentEscrowCommand
        {
            TransactionId    = tx.Id,
            ApprovedByUserId = 0,  // System/automated release
            AdminNote        = message.AdminNote ?? $"SR {message.ServiceRequestId} completed. Auto-release.",
        }, ct);
    }

    public override Task ExecuteRollbackMessage(
        ServiceRequestCompletedMessage message, AizenMessageError ex, CancellationToken ct)
    {
        _logger.LogError(
            "ServiceRequestCompletedConsumer rollback for SR {SRId}: {Error}",
            message.ServiceRequestId, ex.Message);
        return Task.CompletedTask;
    }
}
