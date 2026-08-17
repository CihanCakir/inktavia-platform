using Aizen.Core.Scheduler;
using Aizen.Core.Scheduler.Abstraction;
using Aizen.Modules.Payment.Domain.Interface.Repository;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Aizen.Modules.Payment.Jobs;

/// <summary>
/// Runs daily at 01:00 UTC. Finds all Sent invoices whose DueDateUtc has passed
/// and transitions them to Overdue.
///
/// ── Scope ──────────────────────────────────────────────────────────────────
///  All invoice types are checked (SalesInvoice, CommissionInvoice, SubscriptionInvoice, etc.).
///  Only Sent status invoices are eligible — Draft, Issued, Overdue, Paid, Cancelled are skipped.
///
/// ── Batch safety ───────────────────────────────────────────────────────────
///  Processes at most Payment:OverdueBatchSize records per run (default 500).
///  If more exist, the next daily run picks them up.
///
/// ── No event published ─────────────────────────────────────────────────────
///  Notification module is responsible for overdue reminder emails, triggered
///  separately by its own scheduler. Invoice status change is the sole output.
///
/// Configuration:
///   Payment:OverdueBatchSize  (default 500)
/// </summary>
public sealed class InvoiceOverdueMarkingJob : AizenRecurringJob
{
    public InvoiceOverdueMarkingJob(
        IAizenSchedulerLogger logger,
        IServiceProvider      serviceProvider)
        : base(logger, serviceProvider) { }

    public override bool   IsActive       => true;
    public override string CronExpression => "0 1 * * *"; // daily 01:00 UTC

    protected override async Task ProcessAsync(CancellationToken cancellationToken)
    {
        using var scope     = ServiceProvider.CreateScope();
        var config          = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var invoiceRepo     = scope.ServiceProvider.GetRequiredService<IInvoiceRepository>();

        var batchSize = config.GetValue<int>("Payment:OverdueBatchSize", 500);
        var now       = DateTime.UtcNow;

        var overdueInvoices = await invoiceRepo.GetSentOverdueAsync(now, batchSize, cancellationToken);

        Logger.WriteConsole(
            $"InvoiceOverdueMarkingJob: found {overdueInvoices.Count} Sent invoices past due date.");

        if (overdueInvoices.Count == 0) return;

        foreach (var invoice in overdueInvoices)
        {
            invoice.MarkOverdue(triggeredByUserId: null);
            invoiceRepo.Update(invoice);
        }

        await invoiceRepo.SaveChangesAsync(cancellationToken);

        Logger.WriteConsole(
            $"InvoiceOverdueMarkingJob: {overdueInvoices.Count} invoices transitioned to Overdue.");
    }
}
