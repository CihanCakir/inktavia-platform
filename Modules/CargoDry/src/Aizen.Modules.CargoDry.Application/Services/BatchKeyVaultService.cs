using System.Security.Cryptography;
using Aizen.Modules.CargoDry.Abstraction.Interface.Service;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Aizen.Modules.CargoDry.Application.Services;

public sealed class BatchKeyVaultService : IBatchKeyVaultService
{
    private readonly IConfiguration _cfg;
    private readonly ILogger<BatchKeyVaultService> _logger;

    public BatchKeyVaultService(IConfiguration cfg, ILogger<BatchKeyVaultService> logger)
    {
        _cfg    = cfg;
        _logger = logger;
    }

    public Task<string> GetKeyAsync(string batchCode, CancellationToken ct)
    {
        var key = _cfg[$"CargoDry:BatchKeys:{batchCode}"];
        if (string.IsNullOrWhiteSpace(key))
            throw new InvalidOperationException($"Batch key not configured for batch {batchCode}");
        return Task.FromResult(key);
    }

    public Task<string> CreateKeyAsync(string batchCode, CancellationToken ct)
    {
        var rawKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        _logger.LogInformation(
            "Generated new batch key for {BatchCode}. Add to config: CargoDry:BatchKeys:{BatchCode}={Key}",
            batchCode, batchCode, rawKey);
        return Task.FromResult(rawKey);
    }
}
