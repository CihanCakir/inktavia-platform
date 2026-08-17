namespace Aizen.Modules.CargoDry.Abstraction.Interface.Service;

public interface IBatchKeyVaultService
{
    Task<string> GetKeyAsync(string batchCode, CancellationToken ct = default);
    Task<string> CreateKeyAsync(string batchCode, CancellationToken ct = default);
}
