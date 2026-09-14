using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Bff.AdminPanel.Application.Common.Warnings;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.CargoDry.Command.RequestCargoDryProductImageUploadUrlBff;

/// <summary>
/// CargoDry supply flow: requests a FileStorage presigned PUT URL (OwnerModule=CargoDry) for a product image.
/// The browser PUTs the bytes directly, then registers the FileId via POST products/{code}/images.
/// </summary>
public sealed class RequestCargoDryProductImageUploadUrlBffCommand
    : AizenCommand<CargoDryProductImageUploadUrlBffResponse>
{
    public string ProductCode   { get; init; } = default!;
    public string FileName      { get; init; } = default!;
    public string ContentType   { get; init; } = default!;
    public long   FileSizeBytes { get; init; }
}

public sealed class CargoDryProductImageUploadUrlBffResponse
{
    public string? FileId            { get; set; }
    public string? UploadUrl         { get; set; }
    public string? UploadSessionCode { get; set; }
    public DateTime? ExpiresAt       { get; set; }
    public List<string> Warnings     { get; set; } = new();
}

public sealed class RequestCargoDryProductImageUploadUrlBffCommandHandler
    : AizenCommandHandler<RequestCargoDryProductImageUploadUrlBffCommand, CargoDryProductImageUploadUrlBffResponse>
{
    private readonly IFileStorageRemoteCall _fileStorage;

    public RequestCargoDryProductImageUploadUrlBffCommandHandler(IFileStorageRemoteCall fileStorage)
        => _fileStorage = fileStorage;

    public override async Task<CargoDryProductImageUploadUrlBffResponse?> Handle(
        RequestCargoDryProductImageUploadUrlBffCommand request, CancellationToken ct)
    {
        var response = new CargoDryProductImageUploadUrlBffResponse();

        var result = await _fileStorage.CreateDocumentUploadSession(new CreateDocumentUploadSessionRequest
        {
            OriginalFileName = request.FileName,
            ContentType      = request.ContentType,
            SizeInBytes      = request.FileSizeBytes,
            OwnerModule      = "CargoDry",
        });

        if (result?.Header?.IsSuccess != true || result.Body is null)
        {
            response.Warnings.Add(
                $"FileStorage: {result?.Header?.ErrorMessage ?? "Failed to create upload session."}");
            return response;
        }

        response.FileId            = result.Body.FileId.ToString();
        response.UploadUrl         = result.Body.UploadUrl;
        response.UploadSessionCode = result.Body.UploadSessionCode;
        response.ExpiresAt         = result.Body.ExpiresAt;
        return response;
    }
}
