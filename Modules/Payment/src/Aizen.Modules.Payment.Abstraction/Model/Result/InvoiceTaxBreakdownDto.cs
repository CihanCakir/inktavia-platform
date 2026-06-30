namespace Aizen.Modules.Payment.Abstraction.Model.Result;

public sealed record InvoiceTaxBreakdownDto(
    long    Id,
    string  TaxType,
    decimal TaxRate,
    decimal TaxableAmount,
    decimal TaxAmount
);
