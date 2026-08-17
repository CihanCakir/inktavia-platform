using Aizen.Modules.Payment.Abstraction.Enum;

namespace Aizen.Modules.Payment.Abstraction.Model.Result;

public sealed record InvoiceLineDto(
    long              Id,
    int               LineNumber,
    InvoiceLineType   LineType,
    string            Description,
    string?           ProductCode,
    string?           ServiceCategoryCode,
    decimal           Quantity,
    string            UnitCode,
    decimal           UnitPrice,
    decimal           LineSubTotal,
    decimal           DiscountAmount,
    decimal           TaxRate,
    decimal           TaxAmount,
    decimal           LineTotal,
    InvoiceSourceType? SourceType,
    long?             SourceId
);
