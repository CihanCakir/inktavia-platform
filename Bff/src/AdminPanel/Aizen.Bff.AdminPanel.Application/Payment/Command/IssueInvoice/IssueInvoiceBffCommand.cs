using Aizen.Core.CQRS.Message;
using Aizen.Modules.Payment.Abstraction.Model.Result;

namespace Aizen.Bff.AdminPanel.Application.Payment.Command.IssueInvoice;

public sealed class IssueInvoiceBffCommand : AizenCommand<IssueInvoiceBffCommandResponse>
{
    public long Id { get; init; }
}

public sealed class IssueInvoiceBffCommandResponse
{
    public IssueInvoiceResult Result { get; init; } = default!;
}
