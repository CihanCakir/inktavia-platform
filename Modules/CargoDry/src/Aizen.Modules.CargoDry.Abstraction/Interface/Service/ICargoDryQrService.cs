namespace Aizen.Modules.CargoDry.Abstraction.Interface.Service;

public interface ICargoDryQrService
{
    Task<string> SignAsync(string serialNumber, string batchCode, CancellationToken ct = default);
    Task<bool> VerifyAsync(string serialNumber, string batchCode, string signature, CancellationToken ct = default);
    string GenerateSerialNumber();
    byte[] GenerateQrCode(string payload);
}
