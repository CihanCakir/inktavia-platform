namespace Aizen.Modules.Payment.Abstraction.Model.Result;

/// <summary>
/// Returned by IssueInvoiceCommand.
/// InvoiceNumber is the formatted string assigned at issue time (e.g. INV-2026-06-000001).
/// </summary>
public sealed record IssueInvoiceResult(
    long     InvoiceId,
    string   InvoiceNumber,
    DateTime IssueDateUtc
);
