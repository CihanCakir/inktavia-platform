namespace Aizen.Modules.Payment.Abstraction.Model.Result;

/// <summary>
/// Returned by CreateInvoiceDraftCommand.
/// InvoiceId is the auto-generated PK; PublicId is the Guid for external-facing references.
/// InvoiceNumber is null at Draft stage — assigned only after Issue.
/// </summary>
public sealed record CreateInvoiceDraftResult(
    long  InvoiceId,
    Guid? PublicId
);
