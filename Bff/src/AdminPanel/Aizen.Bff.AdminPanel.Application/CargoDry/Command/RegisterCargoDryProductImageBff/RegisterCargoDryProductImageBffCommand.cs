using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients.CargoDry;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;
using Aizen.Modules.CargoDry.Abstraction.Dto;

namespace Aizen.Bff.AdminPanel.Application.CargoDry.Command.RegisterCargoDryProductImageBff;

/// <summary>
/// CargoDry supply flow: after a browser-direct presigned upload, optionally completes the upload session, then
/// registers the resulting FileStorage FileId against the product gallery.
/// </summary>
public sealed class RegisterCargoDryProductImageBffCommand : AizenCommand<CargoDryProductImagesDto>
{
    public string  ProductCode       { get; init; } = default!;
    public Guid    FileId            { get; init; }
    public string? UploadSessionCode { get; init; }
}

public sealed class RegisterCargoDryProductImageBffCommandHandler
    : AizenCommandHandler<RegisterCargoDryProductImageBffCommand, CargoDryProductImagesDto>
{
    private readonly ICargoDryRemoteCall    _remote;
    private readonly IFileStorageRemoteCall _fileStorage;

    public RegisterCargoDryProductImageBffCommandHandler(
        ICargoDryRemoteCall remote, IFileStorageRemoteCall fileStorage)
    {
        _remote      = remote;
        _fileStorage = fileStorage;
    }

    public override async Task<CargoDryProductImagesDto?> Handle(
        RegisterCargoDryProductImageBffCommand request, CancellationToken ct)
    {
        // Finalize the upload session if the client provided its code (mirrors the vessel media register flow).
        if (!string.IsNullOrWhiteSpace(request.UploadSessionCode))
            await _fileStorage.CompleteDocumentUploadSession(
                request.UploadSessionCode, new CompleteDocumentUploadSessionRequest());

        return await _remote.AddProductImageAsync(
            request.ProductCode, new AddProductImageRemoteRequest { FileId = request.FileId }, ct);
    }
}
