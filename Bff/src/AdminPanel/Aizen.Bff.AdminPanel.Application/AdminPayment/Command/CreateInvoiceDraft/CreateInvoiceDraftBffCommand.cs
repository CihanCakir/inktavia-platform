using Aizen.Core.CQRS.Message;
using Aizen.Modules.Payment.Abstraction.Model.Request;
using Aizen.Modules.Payment.Abstraction.Model.Result;

namespace Aizen.Bff.AdminPanel.Application.AdminPayment.Command.CreateInvoiceDraft;

public sealed class CreateInvoiceDraftBffCommand : AizenCommand<CreateInvoiceDraftBffCommandResponse>
{
    public required CreateInvoiceDraftRequest Body { get; init; }
}

public sealed class CreateInvoiceDraftBffCommandResponse
{
    public CreateInvoiceDraftResult Result { get; init; } = default!;
}
