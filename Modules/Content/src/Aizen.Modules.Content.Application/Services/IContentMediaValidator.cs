using Aizen.Modules.Content.Abstraction.Dto;

namespace Aizen.Modules.Content.Application.Services;

/// <summary>
/// Validates that attached media reference real FileStorage assets (B3). Content never stores bytes,
/// only references, so an unknown/invalid asset id must be rejected on the write.
/// </summary>
public interface IContentMediaValidator
{
    /// <summary>
    /// Throws <see cref="Aizen.Core.Infrastructure.Exception.AizenBusinessException"/> when any media item
    /// is missing/ill-formed, points at a non-existent asset, or when FileStorage cannot be reached
    /// (fail-closed — never accept unvalidated references).
    /// </summary>
    Task ValidateAsync(IEnumerable<ContentMediaDto> media, CancellationToken ct = default);
}
