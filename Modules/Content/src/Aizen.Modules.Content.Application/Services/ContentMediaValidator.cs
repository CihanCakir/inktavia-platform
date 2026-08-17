using Aizen.Core.InfoAccessor.Abstraction;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.Content.Abstraction.Dto;
using Aizen.Modules.FileStorage.Abstraction.RemoteCall.File;

namespace Aizen.Modules.Content.Application.Services;

/// <summary>
/// FileStorage-backed media validator (B3). Uses the sanctioned cross-module contract
/// <see cref="IFileStorageRemoteCall"/> (its only synchronous existence check is
/// <c>GetFileMetadata</c>) and forwards the caller's bearer token, mirroring VesselFileStorageService.
/// </summary>
public sealed class ContentMediaValidator : IContentMediaValidator
{
    private readonly IFileStorageRemoteCall _fileStorage;
    private readonly IAizenInfoAccessor _info;

    public ContentMediaValidator(IFileStorageRemoteCall fileStorage, IAizenInfoAccessor info)
    {
        _fileStorage = fileStorage;
        _info = info;
    }

    public async Task ValidateAsync(IEnumerable<ContentMediaDto> media, CancellationToken ct = default)
    {
        var accessToken = _info.UserInfoAccessor?.UserInfo?.AccessToken;

        // Validate each distinct asset id once.
        foreach (var id in media.Select(m => m.FileStorageId).Distinct())
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new AizenBusinessException("Media FileStorageId is required.");

            if (!Guid.TryParse(id, out var fileId))
                throw new AizenBusinessException($"'{id}' is not a valid FileStorage asset id.");

            try
            {
                var metadata = (await _fileStorage.GetFileMetadata(fileId, $"Bearer {accessToken}"))?.Body;
                if (metadata is null)
                    throw new AizenBusinessException($"FileStorage asset '{id}' was not found.");
            }
            catch (AizenBusinessException)
            {
                throw;
            }
            catch (Exception ex)
            {
                // Fail-closed: an unreachable dependency must not silently accept unvalidated references.
                throw new AizenBusinessException(
                    $"FileStorage could not be reached to validate asset '{id}': {ex.Message}");
            }
        }
    }
}
