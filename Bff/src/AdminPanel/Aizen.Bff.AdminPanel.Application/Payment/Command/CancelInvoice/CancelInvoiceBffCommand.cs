using Aizen.Core.CQRS.Message;
using Aizen.Modules.Payment.Abstraction.Model.Result;

namespace Aizen.Bff.AdminPanel.Application.Payment.Command.CancelInvoice;

public sealed class CancelInvoiceBffCommand : AizenCommand<CancelInvoiceBffCommandResponse>
{
    public long Id { get; init; }
}

public sealed class CancelInvoiceBffCommandResponse
{
    public CancelInvoiceResult Result { get; init; } = default!;
}
