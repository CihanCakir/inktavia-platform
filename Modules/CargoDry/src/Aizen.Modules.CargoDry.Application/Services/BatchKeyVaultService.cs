using System.Security.Cryptography;
using Aizen.Core.Security;
using Aizen.Modules.CargoDry.Abstraction.Interface.Service;
using Aizen.Modules.CargoDry.Domain.Interface.Repository;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Aizen.Modules.CargoDry.Application.Services;

/// <summary>
/// Per-batch HMAC signing key vault. Keys are persisted durably (AES-encrypted) on the batch row — config-only storage
/// is a dead end because batch codes are generated at runtime. Resolution order in <see cref="GetKeyAsync"/>:
/// (1) in-memory cache for a key just created in THIS scope (batch not yet saved during generation),
/// (2) the batch row's decrypted <c>SigningKeyEncrypted</c>,
/// (3) legacy <c>CargoDry:BatchKeys:{code}</c> config (keeps seeded dev batches verifiable),
/// else throw. Registered Scoped, so the same instance backs both the generate handler and the QR service within a
/// request — signing during generation resolves the in-flight key from the cache without a DB read of the unsaved batch.
/// </summary>
public sealed class BatchKeyVaultService : IBatchKeyVaultService
{
    // Non-local deployments MUST override via CargoDry__BatchKeyEncryptionKey (see appsettings note). Must be 32 chars
    // (AES-256 key = 32 ASCII bytes) or AizenSecurityHelper.EncryptByAES throws.
    private const string DevEncryptionKeyFallback = "CargoDryBatchKeyEncryptionKey_32";

    private readonly IConfiguration            _cfg;
    private readonly ICargoDryBatchRepository  _batches;
    private readonly ILogger<BatchKeyVaultService> _logger;

    // In-flight keys created this scope, keyed by batch code — visible to signing before the batch is persisted.
    private readonly Dictionary<string, string> _pendingKeys = new(StringComparer.Ordinal);

    public BatchKeyVaultService(
        IConfiguration cfg, ICargoDryBatchRepository batches, ILogger<BatchKeyVaultService> logger)
    {
        _cfg     = cfg;
        _batches = batches;
        _logger  = logger;
    }

    private string EncryptionKey => _cfg["CargoDry:BatchKeyEncryptionKey"] is { Length: > 0 } k
        ? k
        : DevEncryptionKeyFallback;

    public async Task<string> GetKeyAsync(string batchCode, CancellationToken ct = default)
    {
        // (1) Just created in this scope — batch may not be persisted yet (generation path).
        if (_pendingKeys.TryGetValue(batchCode, out var pending))
            return pending;

        // (2) Durable per-batch key on the row.
        var batch = await _batches.GetByCodeAsync(batchCode, ct);
        if (!string.IsNullOrWhiteSpace(batch?.SigningKeyEncrypted))
            return batch!.SigningKeyEncrypted!.DecryptByAES(EncryptionKey);

        // (3) Legacy config fallback for seeded dev batches.
        var configured = _cfg[$"CargoDry:BatchKeys:{batchCode}"];
        if (!string.IsNullOrWhiteSpace(configured))
            return configured;

        throw new InvalidOperationException($"Batch key not configured for batch {batchCode}");
    }

    /// <summary>
    /// Generates a fresh random signing key, caches the raw value for in-scope signing, and returns the ENCRYPTED form
    /// for the caller to persist on the batch row (via <c>CargoDryBatchEntity.SetSigningKey</c>). The raw key never
    /// leaves the vault — the caller stores only the opaque encrypted blob.
    /// </summary>
    public Task<string> CreateKeyAsync(string batchCode, CancellationToken ct = default)
    {
        var rawKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        _pendingKeys[batchCode] = rawKey;
        var encrypted = rawKey.EncryptByAES(EncryptionKey);
        _logger.LogInformation("Generated and persisted new signing key for batch {BatchCode}", batchCode);
        return Task.FromResult(encrypted);
    }
}
