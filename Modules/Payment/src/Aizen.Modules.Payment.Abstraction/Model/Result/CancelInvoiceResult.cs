using Aizen.Modules.Payment.Abstraction.Enum;

namespace Aizen.Modules.Payment.Abstraction.Model.Result;

/// <summary>
/// Returned by CancelInvoiceCommand.
/// NewStatus is always InvoiceStatus.Cancelled for a fresh cancel.
/// </summary>
public sealed record CancelInvoiceResult(
    long          InvoiceId,
    InvoiceStatus NewStatus,
    DateTime      CancelledAtUtc
);
