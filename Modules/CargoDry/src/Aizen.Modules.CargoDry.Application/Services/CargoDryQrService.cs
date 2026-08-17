using System.Security.Cryptography;
using System.Text;
using Aizen.Modules.CargoDry.Abstraction.Interface.Service;
using QRCoder;

namespace Aizen.Modules.CargoDry.Application.Services;

public sealed class CargoDryQrService : ICargoDryQrService
{
    private const string B32 = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
    private readonly IBatchKeyVaultService _keyVault;

    public CargoDryQrService(IBatchKeyVaultService keyVault) => _keyVault = keyVault;

    public async Task<string> SignAsync(string serialNumber, string batchCode, CancellationToken ct)
    {
        var key      = await _keyVault.GetKeyAsync(batchCode, ct);
        var keyBytes = Encoding.UTF8.GetBytes(key);
        var data     = Encoding.UTF8.GetBytes(serialNumber + ":" + batchCode);
        var mac      = HMACSHA256.HashData(keyBytes, data);
        return Convert.ToBase64String(mac)[..16];
    }

    public async Task<bool> VerifyAsync(string serialNumber, string batchCode, string signature, CancellationToken ct)
    {
        var expected      = await SignAsync(serialNumber, batchCode, ct);
        var expectedBytes = Encoding.UTF8.GetBytes(expected);
        var actualBytes   = Encoding.UTF8.GetBytes(signature);
        if (expectedBytes.Length != actualBytes.Length) return false;
        return CryptographicOperations.FixedTimeEquals(expectedBytes, actualBytes);
    }

    public string GenerateSerialNumber()
    {
        Span<byte> buf = stackalloc byte[10];
        RandomNumberGenerator.Fill(buf);
        var chars = new char[16];
        for (var i = 0; i < 16; i++)
        {
            var byteIdx = i * 5 / 8;
            var bitOff  = i * 5 % 8;
            int raw = buf[byteIdx] >> bitOff;
            if (bitOff > 3 && byteIdx + 1 < buf.Length)
                raw |= buf[byteIdx + 1] << (8 - bitOff);
            chars[i] = B32[raw & 0x1F];
        }
        return $"{new string(chars, 0, 4)}-{new string(chars, 4, 4)}-{new string(chars, 8, 4)}-{new string(chars, 12, 4)}";
    }

    public byte[] GenerateQrCode(string payload)
    {
        using var qr   = new QRCodeGenerator();
        var data       = qr.CreateQrCode(payload, QRCodeGenerator.ECCLevel.M);
        using var png  = new PngByteQRCode(data);
        return png.GetGraphic(10);
    }
}
