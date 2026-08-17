using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.ServiceRequests.Command;

/// <summary>S2a — envelope-correct result of a deactivate (the module soft-deletes; the code is kept).</summary>
public sealed class DeletePricingAttributeResult
{
    public bool Success { get; set; } = true;
}

/// <summary>S2a — deactivate a pricing attribute definition by id.</summary>
public sealed class DeletePricingAttributeBffCommand : AizenCommand<DeletePricingAttributeResult>
{
    public long Id { get; init; }
}
