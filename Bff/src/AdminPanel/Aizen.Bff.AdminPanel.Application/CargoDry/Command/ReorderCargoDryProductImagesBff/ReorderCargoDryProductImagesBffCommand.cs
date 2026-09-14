using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients.CargoDry;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;
using Aizen.Modules.CargoDry.Abstraction.Dto;

namespace Aizen.Bff.AdminPanel.Application.CargoDry.Command.ReorderCargoDryProductImagesBff;

/// <summary>CargoDry supply flow: reorders a product's gallery (proxy to the CargoDry module).</summary>
public sealed class ReorderCargoDryProductImagesBffCommand : AizenCommand<CargoDryProductImagesDto>
{
    public string     ProductCode    { get; init; } = default!;
    public List<Guid> OrderedFileIds { get; init; } = new();
}

public sealed class ReorderCargoDryProductImagesBffCommandHandler
    : AizenCommandHandler<ReorderCargoDryProductImagesBffCommand, CargoDryProductImagesDto>
{
    private readonly ICargoDryRemoteCall _remote;
    public ReorderCargoDryProductImagesBffCommandHandler(ICargoDryRemoteCall remote) => _remote = remote;

    public override async Task<CargoDryProductImagesDto?> Handle(
        ReorderCargoDryProductImagesBffCommand request, CancellationToken ct)
        => await _remote.ReorderProductImagesAsync(
            request.ProductCode,
            new ReorderProductImagesRemoteRequest { OrderedFileIds = request.OrderedFileIds }, ct);
}
