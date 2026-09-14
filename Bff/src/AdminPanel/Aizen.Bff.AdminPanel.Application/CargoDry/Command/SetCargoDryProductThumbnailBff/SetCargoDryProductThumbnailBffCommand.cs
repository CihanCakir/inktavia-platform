using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients.CargoDry;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;
using Aizen.Modules.CargoDry.Abstraction.Dto;

namespace Aizen.Bff.AdminPanel.Application.CargoDry.Command.SetCargoDryProductThumbnailBff;

/// <summary>CargoDry supply flow: sets/clears a product thumbnail (proxy to the CargoDry module).</summary>
public sealed class SetCargoDryProductThumbnailBffCommand : AizenCommand<CargoDryProductImagesDto>
{
    public string ProductCode { get; init; } = default!;
    public Guid?  FileId      { get; init; }
}

public sealed class SetCargoDryProductThumbnailBffCommandHandler
    : AizenCommandHandler<SetCargoDryProductThumbnailBffCommand, CargoDryProductImagesDto>
{
    private readonly ICargoDryRemoteCall _remote;
    public SetCargoDryProductThumbnailBffCommandHandler(ICargoDryRemoteCall remote) => _remote = remote;

    public override async Task<CargoDryProductImagesDto?> Handle(
        SetCargoDryProductThumbnailBffCommand request, CancellationToken ct)
        => await _remote.SetProductThumbnailAsync(
            request.ProductCode, new SetProductThumbnailRemoteRequest { FileId = request.FileId }, ct);
}
