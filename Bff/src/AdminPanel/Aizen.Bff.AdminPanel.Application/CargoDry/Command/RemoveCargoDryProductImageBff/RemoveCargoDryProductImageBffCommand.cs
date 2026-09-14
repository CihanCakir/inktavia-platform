using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;
using Aizen.Modules.CargoDry.Abstraction.Dto;

namespace Aizen.Bff.AdminPanel.Application.CargoDry.Command.RemoveCargoDryProductImageBff;

/// <summary>CargoDry supply flow: removes one gallery image from a product (proxy to the CargoDry module).</summary>
public sealed class RemoveCargoDryProductImageBffCommand : AizenCommand<CargoDryProductImagesDto>
{
    public string ProductCode { get; init; } = default!;
    public Guid   FileId      { get; init; }
}

public sealed class RemoveCargoDryProductImageBffCommandHandler
    : AizenCommandHandler<RemoveCargoDryProductImageBffCommand, CargoDryProductImagesDto>
{
    private readonly ICargoDryRemoteCall _remote;
    public RemoveCargoDryProductImageBffCommandHandler(ICargoDryRemoteCall remote) => _remote = remote;

    public override async Task<CargoDryProductImagesDto?> Handle(
        RemoveCargoDryProductImageBffCommand request, CancellationToken ct)
        => await _remote.RemoveProductImageAsync(request.ProductCode, request.FileId, ct);
}
