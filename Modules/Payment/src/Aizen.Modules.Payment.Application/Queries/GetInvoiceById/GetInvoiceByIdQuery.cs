using Aizen.Core.CQRS.Message;
using Aizen.Modules.Payment.Abstraction.Model.Result;

namespace Aizen.Modules.Payment.Application.Queries.GetInvoiceById;

/// <summary>
/// Returns a single invoice by its internal Id.
/// Set IncludeLines = true to populate Lines and TaxBreakdowns in the response.
/// </summary>
public sealed class GetInvoiceByIdQuery : AizenQuery<InvoiceDto>
{
    public required long InvoiceId    { get; init; }
    public          bool IncludeLines { get; init; } = false;
}
