using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.Payment.Domain.Entities.Invoice;
using Aizen.Modules.Payment.Domain.Interface.Repository;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Aizen.Modules.Payment.Application.Services;

/// <summary>
/// Generates formatted invoice numbers using a per-prefix per-month sequence table.
///
/// ── Format ────────────────────────────────────────────────────────────────────
///  {PREFIX}-{YYYY}-{MM}-{NNNNNN}
///  Example: INV-2026-06-000001
///
/// ── Prefix mapping ────────────────────────────────────────────────────────────
///  SalesInvoice       → INV
///  CommissionInvoice  → COM
///  SubscriptionInvoice → SUB
///  CargoDryInvoice    → CDY
///  CreditNote         → CRD
///  RefundInvoice      → REF
///  ProformaInvoice    → PRF
///
/// ── Concurrency strategy ──────────────────────────────────────────────────────
///  Optimistic concurrency: EF tracks UpdatedAtUtc as a WHERE clause on UPDATE.
///  If another process increments first, DbUpdateConcurrencyException is thrown.
///  This service retries up to MaxRetries times with exponential backoff (base 50 ms).
///
/// ── Anti-gap guarantee ────────────────────────────────────────────────────────
///  This service is called ONLY inside IssueInvoiceCommandHandler, never at Draft creation.
///  Cancelled drafts never consume a sequence number.
/// </summary>
[DocumentationInfo("Invoice number service",
    "Concurrency-safe invoice number generator. Reads/creates sequence row per prefix/year/month " +
    "and increments with optimistic concurrency retry.")]
public sealed class InvoiceNumberService
{
    private const int MaxRetries  = 5;
    private const int BaseDelayMs = 50;

    private static readonly IReadOnlyDictionary<InvoiceType, string> PrefixMap =
        new Dictionary<InvoiceType, string>
        {
            [InvoiceType.SalesInvoice]        = "INV",
            [InvoiceType.CommissionInvoice]   = "COM",
            [InvoiceType.SubscriptionInvoice] = "SUB",
            [InvoiceType.CargoDryInvoice]     = "CDY",
            [InvoiceType.CreditNote]          = "CRD",
            [InvoiceType.RefundInvoice]       = "REF",
            [InvoiceType.ProformaInvoice]     = "PRF",
        };

    private readonly IInvoiceNumberSequenceRepository          _sequences;
    private readonly ILogger<InvoiceNumberService>             _logger;

    public InvoiceNumberService(
        IInvoiceNumberSequenceRepository sequences,
        ILogger<InvoiceNumberService>    logger)
    {
        _sequences = sequences;
        _logger    = logger;
    }

    /// <summary>
    /// Resolves the prefix string for the given invoice type.
    /// Throws ArgumentOutOfRangeException for unmapped types.
    /// </summary>
    public static string GetPrefix(InvoiceType invoiceType)
    {
        if (!PrefixMap.TryGetValue(invoiceType, out var prefix))
            throw new ArgumentOutOfRangeException(nameof(invoiceType),
                $"No invoice number prefix is defined for InvoiceType.{invoiceType}.");
        return prefix;
    }

    /// <summary>
    /// Atomically increments the monthly sequence counter and returns a formatted invoice number.
    /// The sequence row is lazily created on the first invoice of each (prefix, year, month).
    ///
    /// Saves the sequence row directly via IInvoiceNumberSequenceRepository.SaveChangesAsync —
    /// this is intentionally separate from the main invoice UnitOfWork so the number is
    /// committed even if the outer transaction rolls back (prevents gaps).
    ///
    /// Caller (IssueInvoiceCommandHandler) should handle AizenBusinessException(5033) and
    /// surface it to the HTTP layer for a retry from the client.
    /// </summary>
    /// <exception cref="AizenBusinessException">
    ///   Thrown with code 5033 (InvoiceNumberConcurrencyExceeded) after MaxRetries failures.
    /// </exception>
    public async Task<string> GenerateAsync(InvoiceType invoiceType, CancellationToken ct = default)
    {
        var prefix = GetPrefix(invoiceType);
        var now    = DateTime.UtcNow;
        var year   = now.Year;
        var month  = now.Month;

        for (var attempt = 1; attempt <= MaxRetries; attempt++)
        {
            try
            {
                var sequence = await _sequences.GetAsync(prefix, year, month, ct);

                if (sequence is null)
                {
                    // First invoice for this (prefix, year, month) — create the row.
                    sequence = InvoiceNumberSequenceEntity.Create(prefix, year, month);
                    await _sequences.AddAsync(sequence, ct);
                }
                else
                {
                    _sequences.Update(sequence);
                }

                sequence.Increment();
                await _sequences.SaveChangesAsync(ct);

                var invoiceNumber = sequence.FormatNumber();
                _logger.LogInformation(
                    "Invoice number generated: {Number} (attempt {Attempt})", invoiceNumber, attempt);

                return invoiceNumber;
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogWarning(ex,
                    "InvoiceNumberService: concurrency conflict on attempt {Attempt}/{Max} " +
                    "for prefix={Prefix} year={Year} month={Month}. Retrying.",
                    attempt, MaxRetries, prefix, year, month);

                if (attempt == MaxRetries)
                {
                    _logger.LogError(
                        "InvoiceNumberService: exceeded {Max} retry attempts for prefix={Prefix}. " +
                        "Invoice number generation aborted.", MaxRetries, prefix);

                    throw new AizenBusinessException((int)PaymentErrorCode.InvoiceNumberConcurrencyExceeded);
                }

                // Exponential backoff: 50ms, 100ms, 200ms, 400ms
                var delayMs = BaseDelayMs * (int)Math.Pow(2, attempt - 1);
                await Task.Delay(delayMs, ct);
            }
        }

        // Unreachable — loop always throws or returns above
        throw new AizenBusinessException((int)PaymentErrorCode.InvoiceNumberConcurrencyExceeded);
    }
}
