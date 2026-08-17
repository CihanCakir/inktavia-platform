# PROMPT B — CargoDry Module: Application Layer

## Context

`Aizen.Modules.CargoDry.Application` projesinde Class1.cs silinecek.
Bu dosya CQRS handlers, security services, scheduler jobs ve MassTransit consumers içerir.
References: Domain + Abstraction projects.

Naming: `Aizen.Modules.CargoDry.Application`

---

## STEP 1 — Security Services

### 1.1 Serial Number Generator

**`Services/CargoDryQrService.cs`**
```csharp
using System.Security.Cryptography;
using System.Text;
using Aizen.Modules.CargoDry.Abstraction.Interface.Service;
using QRCoder;

namespace Aizen.Modules.CargoDry.Application.Services;

public sealed class CargoDryQrService : ICargoDryQrService
{
    // Base32 alphabet — excludes 0,1,O,I (ambiguous characters)
    private const string B32 = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
    private readonly IBatchKeyVaultService _keyVault;

    public CargoDryQrService(IBatchKeyVaultService keyVault) => _keyVault = keyVault;

    // ── Signing ──────────────────────────────────────────────────────────────

    public async Task<string> SignAsync(string serialNumber, string batchCode, CancellationToken ct)
    {
        var key     = await _keyVault.GetKeyAsync(batchCode, ct);
        var keyBytes = Encoding.UTF8.GetBytes(key);
        var data    = Encoding.UTF8.GetBytes(serialNumber + ":" + batchCode);
        var mac     = HMACSHA256.HashData(keyBytes, data);
        return Convert.ToBase64String(mac)[..16]; // 16-char short signature
    }

    public async Task<bool> VerifyAsync(
        string serialNumber, string batchCode, string signature, CancellationToken ct)
    {
        var expected = await SignAsync(serialNumber, batchCode, ct);
        var expectedBytes = Encoding.UTF8.GetBytes(expected);
        var actualBytes   = Encoding.UTF8.GetBytes(signature);

        // FixedTimeEquals — timing attack protection
        if (expectedBytes.Length != actualBytes.Length) return false;
        return CryptographicOperations.FixedTimeEquals(expectedBytes, actualBytes);
    }

    // ── Serial Number Generation ─────────────────────────────────────────────

    public string GenerateSerialNumber()
    {
        // 80-bit entropy — 16 Base32 characters in XXXX-XXXX-XXXX-XXXX format
        Span<byte> buf = stackalloc byte[10];
        RandomNumberGenerator.Fill(buf);
        var chars = new char[16];
        for (var i = 0; i < 16; i++)
        {
            // map 5-bit chunks into Base32 alphabet
            var byteIdx = i * 5 / 8;
            var bitOff  = i * 5 % 8;
            int raw = buf[byteIdx] >> bitOff;
            if (bitOff > 3 && byteIdx + 1 < buf.Length)
                raw |= buf[byteIdx + 1] << (8 - bitOff);
            chars[i] = B32[raw & 0x1F];
        }
        return $"{new string(chars, 0, 4)}-{new string(chars, 4, 4)}-{new string(chars, 8, 4)}-{new string(chars, 12, 4)}";
    }

    // ── QR Code Generation ───────────────────────────────────────────────────

    public byte[] GenerateQrCode(string payload)
    {
        using var qr = new QRCodeGenerator();
        var data = qr.CreateQrCode(payload, QRCodeGenerator.ECCLevel.M);
        using var png = new PngByteQRCode(data);
        return png.GetGraphic(10);
    }
}
```

### 1.2 Activation Token Service

**`Services/ActivationTokenService.cs`**
```csharp
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Aizen.Modules.CargoDry.Abstraction.Interface.Service;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using StackExchange.Redis;

namespace Aizen.Modules.CargoDry.Application.Services;

/// <summary>
/// 5 dakika TTL'li JWT üretir. JTI (UUID v4) Redis'te saklanır.
/// Activate sırasında JTI kontrol edilir + silinir → tek kullanımlık.
/// Aynı anda gelen iki isteği önler (race condition protection).
/// </summary>
public sealed class ActivationTokenService : IActivationTokenService
{
    private const int TtlMinutes = 5;
    private readonly string _secret;
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<ActivationTokenService> _logger;

    public ActivationTokenService(
        IConfiguration cfg,
        IConnectionMultiplexer redis,
        ILogger<ActivationTokenService> logger)
    {
        _secret = cfg["CargoDry:ActivationTokenSecret"]
            ?? throw new InvalidOperationException("CargoDry:ActivationTokenSecret not configured");
        _redis  = redis;
        _logger = logger;
    }

    public string Generate(string serialNumber)
    {
        var jti     = Guid.NewGuid().ToString("N");
        var key     = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secret));
        var handler = new JwtSecurityTokenHandler();
        var token   = handler.CreateToken(new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity([
                new Claim("serial", serialNumber),
                new Claim(JwtRegisteredClaimNames.Jti, jti),
            ]),
            Expires            = DateTime.UtcNow.AddMinutes(TtlMinutes),
            SigningCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256),
        });
        var tokenStr = handler.WriteToken(token);

        // Store JTI in Redis with same TTL — consumed on first use
        var db = _redis.GetDatabase();
        db.StringSet($"cargodry:jti:{jti}", "1", TimeSpan.FromMinutes(TtlMinutes + 1));

        return tokenStr;
    }

    public ActivationTokenClaims? Verify(string token)
    {
        try
        {
            var key     = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secret));
            var handler = new JwtSecurityTokenHandler();
            handler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuer           = false,
                ValidateAudience         = false,
                ValidateLifetime         = true,
                IssuerSigningKey         = key,
                ClockSkew                = TimeSpan.Zero,
            }, out var validated);

            var jwt    = (JwtSecurityToken)validated;
            var serial = jwt.Claims.First(c => c.Type == "serial").Value;
            var jti    = jwt.Claims.First(c => c.Type == JwtRegisteredClaimNames.Jti).Value;

            // Check + consume JTI atomically
            var db     = _redis.GetDatabase();
            var exists = (bool)db.ScriptEvaluate(
                "local v = redis.call('GET', KEYS[1]) if v then redis.call('DEL', KEYS[1]) return 1 else return 0 end",
                [$"cargodry:jti:{jti}"]);

            if (!exists)
            {
                _logger.LogWarning("CargoDry activation token JTI already consumed or expired: {Jti}", jti);
                return null;
            }

            return new ActivationTokenClaims
            {
                SerialNumber = serial,
                Jti          = jti,
                ExpiresAt    = DateTimeOffset.FromUnixTimeSeconds(jwt.Payload.Exp ?? 0),
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "CargoDry activation token verification failed");
            return null;
        }
    }
}
```

### 1.3 Batch Key Vault Service

**`Services/BatchKeyVaultService.cs`**
```csharp
using System.Security.Cryptography;
using Aizen.Modules.CargoDry.Abstraction.Interface.Service;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Aizen.Modules.CargoDry.Application.Services;

/// <summary>
/// MVP: Batch key'leri appsettings'ten okur.
/// Production: Azure Key Vault / AWS KMS ile değiştir.
/// Key format: CargoDry:BatchKeys:{batchCode}
/// </summary>
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
        {
            _logger.LogError("Batch key not found for batch {BatchCode}", batchCode);
            throw new InvalidOperationException($"Batch key not configured for batch {batchCode}");
        }
        return Task.FromResult(key);
    }

    public Task<string> CreateKeyAsync(string batchCode, CancellationToken ct)
    {
        // Üretim ortamında Azure Key Vault'a yaz
        // MVP: 256-bit random key üret + döndür (appsettings'e manuel eklenir)
        var rawKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        _logger.LogInformation(
            "Generated new batch key for {BatchCode}. Add to config: CargoDry:BatchKeys:{BatchCode}={Key}",
            batchCode, batchCode, rawKey);
        return Task.FromResult(rawKey);
    }
}
```

---

## STEP 2 — Commands

### 2.1 ValidateKitCommand (QR / Serial doğrulama)

**`Commands/ValidateKit/ValidateKitCommand.cs`**
```csharp
using Aizen.Core.Cqrs.Abstractions;
using Aizen.Modules.CargoDry.Abstraction.Dto;
using Aizen.Modules.CargoDry.Abstraction.Enum;

namespace Aizen.Modules.CargoDry.Application.Commands.ValidateKit;

public sealed class ValidateKitCommand : AizenCommand<CargoDryKitValidationDto>
{
    public string SerialNumber { get; init; } = default!;
    public string BatchCode    { get; init; } = default!;
    public string? Signature   { get; init; }  // null = serial-only validation (no QR)
    public ActivationSource Source { get; init; } = ActivationSource.MobileApp;
}
```

**`Commands/ValidateKit/ValidateKitCommandValidator.cs`**
```csharp
using FluentValidation;

namespace Aizen.Modules.CargoDry.Application.Commands.ValidateKit;

public sealed class ValidateKitCommandValidator : AbstractValidator<ValidateKitCommand>
{
    public ValidateKitCommandValidator()
    {
        RuleFor(x => x.SerialNumber).NotEmpty().MaximumLength(50);
        RuleFor(x => x.BatchCode).NotEmpty().MaximumLength(30);
    }
}
```

**`Commands/ValidateKit/ValidateKitCommandHandler.cs`**
```csharp
using Aizen.Core.Cqrs.Abstractions;
using Aizen.Modules.CargoDry.Abstraction.Dto;
using Aizen.Modules.CargoDry.Abstraction.Enum;
using Aizen.Modules.CargoDry.Abstraction.Interface.Repository;
using Aizen.Modules.CargoDry.Abstraction.Interface.Service;

namespace Aizen.Modules.CargoDry.Application.Commands.ValidateKit;

public sealed class ValidateKitCommandHandler
    : AizenCommandHandler<ValidateKitCommand, CargoDryKitValidationDto>
{
    private readonly ICargoDryKitRepository     _kits;
    private readonly ICargoDryBatchRepository   _batches;
    private readonly ICargoDryProductRepository _products;
    private readonly ICargoDryQrService         _qrService;
    private readonly IActivationTokenService    _tokenService;

    public ValidateKitCommandHandler(
        ICargoDryKitRepository kits,
        ICargoDryBatchRepository batches,
        ICargoDryProductRepository products,
        ICargoDryQrService qrService,
        IActivationTokenService tokenService)
    {
        _kits         = kits;
        _batches      = batches;
        _products     = products;
        _qrService    = qrService;
        _tokenService = tokenService;
    }

    public override async Task<CargoDryKitValidationDto> Handle(
        ValidateKitCommand request, CancellationToken ct)
    {
        // Layer 1: Batch revoke kontrolü
        var batch = await _batches.GetByCodeAsync(request.BatchCode, ct);
        if (batch is null || batch.IsRevoked)
            return Invalid("BatchRevoked");

        // Layer 2: HMAC imza doğrulama (sadece QR flow'da)
        if (!string.IsNullOrEmpty(request.Signature))
        {
            var sigOk = await _qrService.VerifyAsync(
                request.SerialNumber, request.BatchCode, request.Signature, ct);
            if (!sigOk) return Invalid("InvalidSignature");
        }

        // Layer 3: Kit var mı?
        var kit = await _kits.GetBySerialAsync(request.SerialNumber, ct);
        if (kit is null) return Invalid("KitNotFound");

        // Layer 4: Kit Available mı?
        if (kit.Status != CargoDryKitStatus.Available)
            return Invalid(kit.Status == CargoDryKitStatus.Activated ? "AlreadyActivated" : "KitUnavailable");

        // Layer 5: Ürün aktif mi?
        var product = await _products.GetByCodeAsync(kit.ProductCode, ct);
        if (product is null || !product.IsActive)
            return Invalid("ProductInactive");

        // Layer 6: Activation token üret
        var token      = _tokenService.Generate(kit.SerialNumber);
        var expiresAt  = DateTimeOffset.UtcNow.AddMinutes(5);

        return new CargoDryKitValidationDto
        {
            IsValid         = true,
            ProductName     = product.Name,
            ProductCode     = product.ProductCode,
            ValidityDays    = product.ValidityDays,
            HasSmartDevice  = product.HasSmartDevice,
            ActivationToken = token,
            TokenExpiresAt  = expiresAt,
        };
    }

    private static CargoDryKitValidationDto Invalid(string reason) => new()
    {
        IsValid       = false,
        InvalidReason = reason,
    };
}
```

---

### 2.2 ActivateKitCommand

**`Commands/ActivateKit/ActivateKitCommand.cs`**
```csharp
using Aizen.Core.Cqrs.Abstractions;
using Aizen.Modules.CargoDry.Abstraction.Dto;
using Aizen.Modules.CargoDry.Abstraction.Enum;

namespace Aizen.Modules.CargoDry.Application.Commands.ActivateKit;

public sealed class ActivateKitCommand : AizenCommand<CargoDryKitDto>
{
    public string          ActivationToken { get; init; } = default!;
    public long            VesselId        { get; init; }
    public long            UserId          { get; init; }  // from JWT claims
    public ActivationMethod Method         { get; init; } = ActivationMethod.QrScan;
    public ActivationSource Source         { get; init; } = ActivationSource.MobileApp;
    public string?         DeviceInfo      { get; init; }
    public string?         IpAddress       { get; init; }
}
```

**`Commands/ActivateKit/ActivateKitCommandHandler.cs`**
```csharp
using Aizen.Core.Cqrs.Abstractions;
using Aizen.Core.Messagebus.Abstractions;
using Aizen.Modules.CargoDry.Abstraction.Dto;
using Aizen.Modules.CargoDry.Abstraction.Enum;
using Aizen.Modules.CargoDry.Abstraction.Interface.Repository;
using Aizen.Modules.CargoDry.Abstraction.Interface.Service;
using Aizen.Modules.CargoDry.Abstraction.Message;
using Aizen.Modules.CargoDry.Domain.Entities;

namespace Aizen.Modules.CargoDry.Application.Commands.ActivateKit;

public sealed class ActivateKitCommandHandler
    : AizenCommandHandler<ActivateKitCommand, CargoDryKitDto>
{
    private readonly IActivationTokenService    _tokenService;
    private readonly ICargoDryKitRepository     _kits;
    private readonly ICargoDryProductRepository _products;
    private readonly IAizenMessagePublisher     _publisher;

    public ActivateKitCommandHandler(
        IActivationTokenService tokenService,
        ICargoDryKitRepository kits,
        ICargoDryProductRepository products,
        IAizenMessagePublisher publisher)
    {
        _tokenService = tokenService;
        _kits         = kits;
        _products     = products;
        _publisher    = publisher;
    }

    public override async Task<CargoDryKitDto> Handle(
        ActivateKitCommand request, CancellationToken ct)
    {
        // Step 1: Activation token'ı doğrula + consume et (Redis JTI blacklist)
        var claims = _tokenService.Verify(request.ActivationToken)
            ?? throw new InvalidOperationException("Invalid or expired activation token");

        // Step 2: Kit'i al
        var kit = await _kits.GetBySerialAsync(claims.SerialNumber, ct)
            ?? throw new InvalidOperationException($"Kit not found: {claims.SerialNumber}");

        // Step 3: Ürün bilgisi
        var product = await _products.GetByCodeAsync(kit.ProductCode, ct)
            ?? throw new InvalidOperationException($"Product not found: {kit.ProductCode}");

        // Step 4: Eğer aynı vessel + product'ta aktif bir kit varsa → Renewed'e çek
        var existingActiveKit = await _kits.GetActiveByVesselAsync(request.VesselId, kit.ProductCode, ct);
        if (existingActiveKit is not null)
        {
            existingActiveKit.MarkExpired(); // Renewed ile kapatılıyor
            // Renewed status set edilir
        }

        // Step 5: Atomic activate (domain method — throws if not Available)
        kit.Activate(request.UserId, request.VesselId, product.ValidityDays);

        // Step 6: Activation log
        var log = CargoDryActivationLogEntity.Create(
            kit.Id, request.UserId, request.VesselId,
            request.Method, request.Source,
            request.DeviceInfo, request.IpAddress);

        await _kits.SaveChangesAsync(ct);

        // Step 7: Integration event
        await _publisher.PublishAsync(new CargoDryKitActivatedMessage
        {
            KitId        = kit.Id,
            KitCode      = kit.KitCode,
            SerialNumber = kit.SerialNumber,
            ProductName  = product.Name,
            OwnerUserId  = request.UserId,
            VesselId     = request.VesselId,
            ActivatedAt  = kit.ActivatedAt!.Value,
            ExpiresAt    = kit.ExpiresAt!.Value,
            ValidityDays = product.ValidityDays,
        }, ct);

        return MapToDto(kit, product);
    }

    private static CargoDryKitDto MapToDto(
        Aizen.Modules.CargoDry.Domain.Entities.CargoDryKitEntity kit,
        Aizen.Modules.CargoDry.Domain.Entities.CargoDryProductEntity product)
        => new()
        {
            Id                = kit.Id,
            SerialNumber      = kit.SerialNumber,
            KitCode           = kit.KitCode,
            ProductCode       = kit.ProductCode,
            ProductName       = product.Name,
            BatchCode         = kit.BatchCode,
            Status            = kit.Status,
            OwnerUserId       = kit.OwnerUserId,
            VesselId          = kit.VesselId,
            ActivatedAt       = kit.ActivatedAt,
            ExpiresAt         = kit.ExpiresAt,
            EfficiencyPercent = kit.EfficiencyPercent,
            DaysUntilExpiry   = kit.DaysUntilExpiry,
            RenewalCount      = kit.RenewalCount,
            ManufacturedAt    = kit.ManufacturedAt,
        };
}
```

---

### 2.3 GenerateBatchCommand (Admin)

**`Commands/GenerateBatch/GenerateBatchCommand.cs`**
```csharp
using Aizen.Core.Cqrs.Abstractions;
using Aizen.Modules.CargoDry.Abstraction.Dto;

namespace Aizen.Modules.CargoDry.Application.Commands.GenerateBatch;

public sealed class GenerateBatchCommand : AizenCommand<GenerateBatchResultDto>
{
    public string ProductCode { get; init; } = default!;
    public int    Count       { get; init; }  // Max 5000 per batch
    public long   AdminUserId { get; init; }
}
```

**`Commands/GenerateBatch/GenerateBatchCommandValidator.cs`**
```csharp
using FluentValidation;

namespace Aizen.Modules.CargoDry.Application.Commands.GenerateBatch;

public sealed class GenerateBatchCommandValidator : AbstractValidator<GenerateBatchCommand>
{
    public GenerateBatchCommandValidator()
    {
        RuleFor(x => x.ProductCode).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Count).InclusiveBetween(1, 5000);
        RuleFor(x => x.AdminUserId).GreaterThan(0);
    }
}
```

**`Commands/GenerateBatch/GenerateBatchCommandHandler.cs`**
```csharp
using System.Text;
using Aizen.Core.Cqrs.Abstractions;
using Aizen.Modules.CargoDry.Abstraction.Dto;
using Aizen.Modules.CargoDry.Abstraction.Interface.Repository;
using Aizen.Modules.CargoDry.Abstraction.Interface.Service;
using Aizen.Modules.CargoDry.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace Aizen.Modules.CargoDry.Application.Commands.GenerateBatch;

public sealed class GenerateBatchCommandHandler
    : AizenCommandHandler<GenerateBatchCommand, GenerateBatchResultDto>
{
    private readonly ICargoDryBatchRepository   _batches;
    private readonly ICargoDryKitRepository     _kits;
    private readonly ICargoDryProductRepository _products;
    private readonly ICargoDryQrService         _qrService;
    private readonly IBatchKeyVaultService      _keyVault;
    private readonly ILogger<GenerateBatchCommandHandler> _logger;

    public GenerateBatchCommandHandler(
        ICargoDryBatchRepository batches,
        ICargoDryKitRepository kits,
        ICargoDryProductRepository products,
        ICargoDryQrService qrService,
        IBatchKeyVaultService keyVault,
        ILogger<GenerateBatchCommandHandler> logger)
    {
        _batches  = batches;
        _kits     = kits;
        _products = products;
        _qrService = qrService;
        _keyVault  = keyVault;
        _logger   = logger;
    }

    public override async Task<GenerateBatchResultDto> Handle(
        GenerateBatchCommand request, CancellationToken ct)
    {
        var product = await _products.GetByCodeAsync(request.ProductCode, ct)
            ?? throw new InvalidOperationException($"Product not found: {request.ProductCode}");

        // Batch code: YYYYMM-{productCode}-{random4}
        var batchCode = $"{DateTime.UtcNow:yyyyMM}-{product.ProductCode[..Math.Min(4, product.ProductCode.Length)]}-{Guid.NewGuid().ToString("N")[..4].ToUpperInvariant()}";

        // Batch key üret + Key Vault'a kaydet
        var batchKey = await _keyVault.CreateKeyAsync(batchCode, ct);

        var batch = CargoDryBatchEntity.Create(batchCode, product.ProductCode, request.Count, request.AdminUserId);
        await _batches.AddAsync(batch, ct);

        // Kit'leri oluştur
        var kits = new List<CargoDryKitEntity>(request.Count);
        var csvBuilder = new StringBuilder("SerialNumber,KitCode,BatchCode,ProductCode,QrPayload\n");

        for (var i = 0; i < request.Count; i++)
        {
            var serial   = _qrService.GenerateSerialNumber();
            var kitCode  = $"CD-{serial[..4]}{serial[5..9]}"; // CDXXXXXXXX
            var sig      = await _qrService.SignAsync(serial, batchCode, ct);
            var qrPayload = $"https://app.inktavia.com/activate?s={Uri.EscapeDataString(serial)}&b={batchCode}&sig={Uri.EscapeDataString(sig)}";
            var kit = CargoDryKitEntity.Create(serial, kitCode, qrPayload, product.ProductCode, batchCode);
            kits.Add(kit);
            csvBuilder.AppendLine($"{serial},{kitCode},{batchCode},{product.ProductCode},{qrPayload}");
        }

        await _kits.AddRangeAsync(kits, ct);

        // TODO: Upload QR ZIP + Excel to FileStorage, set batch.SetFileRefs(...)
        // MVP: Return placeholder URLs
        batch.SetFileRefs(
            $"cargodry/batches/{batchCode}/qr-codes.zip",
            $"cargodry/batches/{batchCode}/serial-list.csv");
        await _batches.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Generated batch {BatchCode}: {Count} kits for product {ProductCode}",
            batchCode, request.Count, product.ProductCode);

        return new GenerateBatchResultDto
        {
            BatchCode      = batchCode,
            GeneratedCount = request.Count,
            QrZipFileUrl   = batch.QrZipFileRef!,
            ExcelFileUrl   = batch.ExcelFileRef!,
        };
    }
}
```

---

### 2.4 RevokeKitCommand (Admin)

**`Commands/RevokeKit/RevokeKitCommand.cs`**
```csharp
using Aizen.Core.Cqrs.Abstractions;

namespace Aizen.Modules.CargoDry.Application.Commands.RevokeKit;

public sealed class RevokeKitCommand : AizenCommand<bool>
{
    public long   KitId       { get; init; }
    public string Reason      { get; init; } = default!;
    public long   AdminUserId { get; init; }
}
```

**`Commands/RevokeKit/RevokeKitCommandHandler.cs`**
```csharp
using Aizen.Core.Cqrs.Abstractions;
using Aizen.Core.Messagebus.Abstractions;
using Aizen.Modules.CargoDry.Abstraction.Interface.Repository;
using Aizen.Modules.CargoDry.Abstraction.Message;

namespace Aizen.Modules.CargoDry.Application.Commands.RevokeKit;

public sealed class RevokeKitCommandHandler : AizenCommandHandler<RevokeKitCommand, bool>
{
    private readonly ICargoDryKitRepository _kits;
    private readonly IAizenMessagePublisher _publisher;

    public RevokeKitCommandHandler(ICargoDryKitRepository kits, IAizenMessagePublisher publisher)
    {
        _kits      = kits;
        _publisher = publisher;
    }

    public override async Task<bool> Handle(RevokeKitCommand request, CancellationToken ct)
    {
        var kit = await _kits.GetByIdAsync(request.KitId, ct)
            ?? throw new InvalidOperationException($"Kit {request.KitId} not found");

        kit.Revoke(request.Reason);
        await _kits.SaveChangesAsync(ct);

        await _publisher.PublishAsync(new CargoDryKitRevokedMessage
        {
            KitId       = kit.Id,
            KitCode     = kit.KitCode,
            OwnerUserId = kit.OwnerUserId,
            VesselId    = kit.VesselId,
            Reason      = request.Reason,
        }, ct);

        return true;
    }
}
```

---

### 2.5 RenewKitCommand (Commerce integration)

**`Commands/RenewKit/RenewKitCommand.cs`**
```csharp
using Aizen.Core.Cqrs.Abstractions;
using Aizen.Modules.CargoDry.Abstraction.Dto;
using Aizen.Modules.CargoDry.Abstraction.Enum;

namespace Aizen.Modules.CargoDry.Application.Commands.RenewKit;

public sealed class RenewKitCommand : AizenCommand<CargoDryKitDto>
{
    public long        KitId      { get; init; }
    public int         AddedDays  { get; init; }
    public RenewalType Type       { get; init; }
    public string?     PaymentRef { get; init; }
    public long?       AdminUserId { get; init; }
}
```

**`Commands/RenewKit/RenewKitCommandHandler.cs`**
```csharp
using Aizen.Core.Cqrs.Abstractions;
using Aizen.Core.Messagebus.Abstractions;
using Aizen.Modules.CargoDry.Abstraction.Dto;
using Aizen.Modules.CargoDry.Abstraction.Interface.Repository;
using Aizen.Modules.CargoDry.Abstraction.Message;
using Aizen.Modules.CargoDry.Domain.Entities;

namespace Aizen.Modules.CargoDry.Application.Commands.RenewKit;

public sealed class RenewKitCommandHandler : AizenCommandHandler<RenewKitCommand, CargoDryKitDto>
{
    private readonly ICargoDryKitRepository     _kits;
    private readonly ICargoDryProductRepository _products;
    private readonly IAizenMessagePublisher     _publisher;

    public RenewKitCommandHandler(
        ICargoDryKitRepository kits,
        ICargoDryProductRepository products,
        IAizenMessagePublisher publisher)
    {
        _kits      = kits;
        _products  = products;
        _publisher = publisher;
    }

    public override async Task<CargoDryKitDto> Handle(RenewKitCommand request, CancellationToken ct)
    {
        var kit = await _kits.GetByIdAsync(request.KitId, ct)
            ?? throw new InvalidOperationException($"Kit {request.KitId} not found");

        var product = await _products.GetByCodeAsync(kit.ProductCode, ct)!;

        kit.Renew(request.AddedDays, request.PaymentRef ?? string.Empty);

        var renewal = CargoDryRenewalEntity.Create(
            kit.Id, kit.OwnerUserId!.Value,
            kit.ExpiresAt!.Value, request.AddedDays,
            request.Type, request.PaymentRef, request.AdminUserId);

        await _kits.SaveChangesAsync(ct);

        await _publisher.PublishAsync(new CargoDryKitRenewedMessage
        {
            KitId        = kit.Id,
            KitCode      = kit.KitCode,
            OwnerUserId  = kit.OwnerUserId!.Value,
            NewExpiresAt = kit.ExpiresAt!.Value,
            RenewalType  = request.Type.ToString(),
        }, ct);

        return new CargoDryKitDto
        {
            Id                = kit.Id,
            SerialNumber      = kit.SerialNumber,
            KitCode           = kit.KitCode,
            ProductCode       = kit.ProductCode,
            ProductName       = product?.Name ?? kit.ProductCode,
            BatchCode         = kit.BatchCode,
            Status            = kit.Status,
            OwnerUserId       = kit.OwnerUserId,
            VesselId          = kit.VesselId,
            ActivatedAt       = kit.ActivatedAt,
            ExpiresAt         = kit.ExpiresAt,
            EfficiencyPercent = kit.EfficiencyPercent,
            DaysUntilExpiry   = kit.DaysUntilExpiry,
            RenewalCount      = kit.RenewalCount,
            ManufacturedAt    = kit.ManufacturedAt,
        };
    }
}
```

---

## STEP 3 — Queries

### 3.1 GetMyKitsQuery

**`Queries/GetMyKits/GetMyKitsQuery.cs`**
```csharp
using Aizen.Core.Cqrs.Abstractions;
using Aizen.Modules.CargoDry.Abstraction.Dto;

namespace Aizen.Modules.CargoDry.Application.Queries.GetMyKits;

public sealed class GetMyKitsQuery : AizenQuery<List<CargoDryKitDto>>
{
    public long UserId { get; init; }
}
```

**`Queries/GetMyKits/GetMyKitsQueryHandler.cs`**
```csharp
using Aizen.Core.Cqrs.Abstractions;
using Aizen.Modules.CargoDry.Abstraction.Dto;
using Aizen.Modules.CargoDry.Abstraction.Interface.Repository;

namespace Aizen.Modules.CargoDry.Application.Queries.GetMyKits;

public sealed class GetMyKitsQueryHandler
    : AizenQueryHandler<GetMyKitsQuery, List<CargoDryKitDto>>
{
    private readonly ICargoDryKitRepository     _kits;
    private readonly ICargoDryProductRepository _products;

    public GetMyKitsQueryHandler(
        ICargoDryKitRepository kits, ICargoDryProductRepository products)
    {
        _kits     = kits;
        _products = products;
    }

    public override async Task<List<CargoDryKitDto>> Handle(
        GetMyKitsQuery request, CancellationToken ct)
    {
        var kits      = await _kits.GetByOwnerAsync(request.UserId, ct);
        var allProducts = await _products.GetAllActiveAsync(ct);
        var productMap  = allProducts.ToDictionary(p => p.ProductCode);

        return kits.Select(k => new CargoDryKitDto
        {
            Id                = k.Id,
            SerialNumber      = k.SerialNumber,
            KitCode           = k.KitCode,
            ProductCode       = k.ProductCode,
            ProductName       = productMap.TryGetValue(k.ProductCode, out var p) ? p.Name : k.ProductCode,
            BatchCode         = k.BatchCode,
            Status            = k.Status,
            OwnerUserId       = k.OwnerUserId,
            VesselId          = k.VesselId,
            ActivatedAt       = k.ActivatedAt,
            ExpiresAt         = k.ExpiresAt,
            EfficiencyPercent = k.EfficiencyPercent,
            DaysUntilExpiry   = k.DaysUntilExpiry,
            RenewalCount      = k.RenewalCount,
            ManufacturedAt    = k.ManufacturedAt,
        }).ToList();
    }
}
```

### 3.2 GetAdminKitListQuery

**`Queries/GetAdminKitList/GetAdminKitListQuery.cs`**
```csharp
using Aizen.Core.Cqrs.Abstractions;
using Aizen.Modules.CargoDry.Abstraction.Dto;
using Aizen.Modules.CargoDry.Abstraction.Enum;

namespace Aizen.Modules.CargoDry.Application.Queries.GetAdminKitList;

public sealed class GetAdminKitListQuery : AizenQuery<GetAdminKitListResponse>
{
    public CargoDryKitStatus? Status  { get; init; }
    public string?            Search  { get; init; }
    public int                Page    { get; init; } = 1;
    public int                PageSize { get; init; } = 25;
}

public sealed class GetAdminKitListResponse
{
    public List<CargoDryKitDto> Items { get; init; } = [];
    public int Total  { get; init; }
    public int Page   { get; init; }
    public int PageSize { get; init; }
}
```

**`Queries/GetAdminKitList/GetAdminKitListQueryHandler.cs`**
```csharp
using Aizen.Core.Cqrs.Abstractions;
using Aizen.Modules.CargoDry.Abstraction.Dto;
using Aizen.Modules.CargoDry.Abstraction.Interface.Repository;

namespace Aizen.Modules.CargoDry.Application.Queries.GetAdminKitList;

public sealed class GetAdminKitListQueryHandler
    : AizenQueryHandler<GetAdminKitListQuery, GetAdminKitListResponse>
{
    private readonly ICargoDryKitRepository     _kits;
    private readonly ICargoDryProductRepository _products;

    public GetAdminKitListQueryHandler(
        ICargoDryKitRepository kits, ICargoDryProductRepository products)
    {
        _kits     = kits;
        _products = products;
    }

    public override async Task<GetAdminKitListResponse> Handle(
        GetAdminKitListQuery request, CancellationToken ct)
    {
        var skip = (request.Page - 1) * request.PageSize;
        var (items, total) = await _kits.GetPagedAsync(
            request.Status, request.Search, skip, request.PageSize, ct);

        var allProducts = await _products.GetAllActiveAsync(ct);
        var productMap  = allProducts.ToDictionary(p => p.ProductCode);

        return new GetAdminKitListResponse
        {
            Items = items.Select(k => new CargoDryKitDto
            {
                Id                = k.Id,
                SerialNumber      = k.SerialNumber,
                KitCode           = k.KitCode,
                ProductCode       = k.ProductCode,
                ProductName       = productMap.TryGetValue(k.ProductCode, out var p) ? p.Name : k.ProductCode,
                BatchCode         = k.BatchCode,
                Status            = k.Status,
                OwnerUserId       = k.OwnerUserId,
                VesselId          = k.VesselId,
                ActivatedAt       = k.ActivatedAt,
                ExpiresAt         = k.ExpiresAt,
                EfficiencyPercent = k.EfficiencyPercent,
                DaysUntilExpiry   = k.DaysUntilExpiry,
                RenewalCount      = k.RenewalCount,
                ManufacturedAt    = k.ManufacturedAt,
            }).ToList(),
            Total    = total,
            Page     = request.Page,
            PageSize = request.PageSize,
        };
    }
}
```

### 3.3 GetCargoDryStatsQuery

**`Queries/GetCargoDryStats/GetCargoDryStatsQuery.cs`**
```csharp
using Aizen.Core.Cqrs.Abstractions;
using Aizen.Modules.CargoDry.Abstraction.Dto;

namespace Aizen.Modules.CargoDry.Application.Queries.GetCargoDryStats;

public sealed class GetCargoDryStatsQuery : AizenQuery<CargoDryStatsDto> { }
```

**`Queries/GetCargoDryStats/GetCargoDryStatsQueryHandler.cs`**
```csharp
using Aizen.Core.Cqrs.Abstractions;
using Aizen.Modules.CargoDry.Abstraction.Dto;
using Aizen.Modules.CargoDry.Abstraction.Interface.Repository;

namespace Aizen.Modules.CargoDry.Application.Queries.GetCargoDryStats;

public sealed class GetCargoDryStatsQueryHandler
    : AizenQueryHandler<GetCargoDryStatsQuery, CargoDryStatsDto>
{
    private readonly ICargoDryKitRepository   _kits;
    private readonly ICargoDryBatchRepository _batches;

    public GetCargoDryStatsQueryHandler(
        ICargoDryKitRepository kits, ICargoDryBatchRepository batches)
    {
        _kits    = kits;
        _batches = batches;
    }

    public override async Task<CargoDryStatsDto> Handle(
        GetCargoDryStatsQuery request, CancellationToken ct)
    {
        var stats   = await _kits.GetStatsAsync(ct);
        var batches = await _batches.GetAllAsync(ct);

        var activeBatchCount   = batches.Count(b => !b.IsRevoked);
        var totalRenewals      = 0; // TODO: from renewals repo
        var renewalRate        = stats.Active > 0
            ? (double)totalRenewals / stats.Active * 100
            : 0;

        return new CargoDryStatsDto
        {
            TotalKits            = stats.Total,
            AvailableKits        = stats.Available,
            ActiveKits           = stats.Active,
            ExpiringKits         = stats.Expiring,
            ExpiredKits          = stats.Expired,
            RevokedKits          = stats.Revoked,
            TodayActivations     = stats.TodayActivations,
            TotalBatches         = activeBatchCount,
            RenewalRatePercent   = Math.Round(renewalRate, 1),
        };
    }
}
```

---

## STEP 4 — Scheduler Jobs

### 4.1 Kit Expiry Reminder Job

**`Jobs/KitExpiryReminderJob.cs`**
```csharp
using Aizen.Core.Messagebus.Abstractions;
using Aizen.Modules.CargoDry.Abstraction.Interface.Repository;
using Aizen.Modules.CargoDry.Abstraction.Message;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Aizen.Modules.CargoDry.Application.Jobs;

/// <summary>
/// Her gün 09:00 UTC çalışır.
/// 30, 7 ve 1 gün kala expiry reminder event'i yayınlar.
/// Notification modülü tüketir → push + in-app notification gönderir.
/// </summary>
public sealed class KitExpiryReminderJob : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<KitExpiryReminderJob> _logger;

    private static readonly int[] ReminderDays = [30, 7, 1];

    public KitExpiryReminderJob(IServiceScopeFactory sf, ILogger<KitExpiryReminderJob> logger)
    {
        _scopeFactory = sf;
        _logger       = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var now      = DateTimeOffset.UtcNow;
            var next9am  = now.Date.AddDays(now.Hour >= 9 ? 1 : 0).AddHours(9);
            var delay    = next9am - now;
            await Task.Delay(delay, stoppingToken);

            _logger.LogInformation("KitExpiryReminderJob starting at {Time}", DateTimeOffset.UtcNow);
            try
            {
                await RunAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "KitExpiryReminderJob failed");
            }
        }
    }

    private async Task RunAsync(CancellationToken ct)
    {
        using var scope     = _scopeFactory.CreateScope();
        var kits            = scope.ServiceProvider.GetRequiredService<ICargoDryKitRepository>();
        var publisher       = scope.ServiceProvider.GetRequiredService<IAizenMessagePublisher>();

        foreach (var days in ReminderDays)
        {
            var expiring = await kits.GetExpiringAsync(days, ct);
            foreach (var kit in expiring.Where(k => k.DaysUntilExpiry == days))
            {
                await publisher.PublishAsync(new CargoDryKitExpiringMessage
                {
                    KitId       = kit.Id,
                    KitCode     = kit.KitCode,
                    ProductName = kit.ProductCode,
                    OwnerUserId = kit.OwnerUserId!.Value,
                    VesselId    = kit.VesselId!.Value,
                    ExpiresAt   = kit.ExpiresAt!.Value,
                    DaysLeft    = days,
                }, ct);
            }
        }
    }
}
```

### 4.2 Kit Expired Marking Job

**`Jobs/KitExpiredMarkingJob.cs`**
```csharp
using Aizen.Core.Messagebus.Abstractions;
using Aizen.Modules.CargoDry.Abstraction.Interface.Repository;
using Aizen.Modules.CargoDry.Abstraction.Message;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Aizen.Modules.CargoDry.Application.Jobs;

/// <summary>Her saat çalışır. Süresi geçmiş ama Activated statüsünde kalan kitleri Expired'a çeker.</summary>
public sealed class KitExpiredMarkingJob : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<KitExpiredMarkingJob> _logger;

    public KitExpiredMarkingJob(IServiceScopeFactory sf, ILogger<KitExpiredMarkingJob> logger)
    {
        _scopeFactory = sf;
        _logger       = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromHours(1));
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await RunAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "KitExpiredMarkingJob failed");
            }
        }
    }

    private async Task RunAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var kits        = scope.ServiceProvider.GetRequiredService<ICargoDryKitRepository>();
        var publisher   = scope.ServiceProvider.GetRequiredService<IAizenMessagePublisher>();

        var expired = await kits.GetExpiredUnmarkedAsync(ct);
        _logger.LogInformation("KitExpiredMarkingJob: marking {Count} kits as expired", expired.Count);

        foreach (var kit in expired)
        {
            kit.MarkExpired();
            await publisher.PublishAsync(new CargoDryKitExpiredMessage
            {
                KitId       = kit.Id,
                KitCode     = kit.KitCode,
                OwnerUserId = kit.OwnerUserId!.Value,
                VesselId    = kit.VesselId!.Value,
            }, ct);
        }

        await kits.SaveChangesAsync(ct);
    }
}
```

---

## STEP 5 — MassTransit Consumer

### 5.1 CommerceOrderCompletedConsumer (Renewal trigger)

**`Consumers/CommerceOrderCompletedConsumer.cs`**
```csharp
using Aizen.Core.Messagebus.Abstractions;
using Aizen.Modules.CargoDry.Abstraction.Enum;
using Aizen.Modules.CargoDry.Application.Commands.RenewKit;
using Aizen.Modules.Commerce.Abstraction.Message;  // Commerce.Abstraction'dan
using MediatR;
using Microsoft.Extensions.Logging;

namespace Aizen.Modules.CargoDry.Application.Consumers;

/// <summary>
/// Commerce modülü bir sipariş tamamlandığında yayınlar.
/// Eğer sipariş CargoDry renewal item içeriyorsa → RenewKitCommand tetikler.
/// </summary>
public sealed class CommerceOrderCompletedConsumer
    : AizenBaseMessageConsumer<CommerceOrderCompletedMessage>
{
    private readonly IMediator _mediator;
    private readonly ILogger<CommerceOrderCompletedConsumer> _logger;

    public CommerceOrderCompletedConsumer(
        IMediator mediator, ILogger<CommerceOrderCompletedConsumer> logger)
    {
        _mediator = mediator;
        _logger   = logger;
    }

    protected override Task<bool> ExecutePrepareMessage(
        CommerceOrderCompletedMessage message, CancellationToken ct)
        => Task.FromResult(true);

    protected override async Task ExecuteCommitMessage(
        CommerceOrderCompletedMessage message, CancellationToken ct)
    {
        // Commerce order'da CargoDry renewal satırı var mı?
        var renewalItems = message.Items
            .Where(i => i.ItemType == "CargoDryRenewal" && i.ReferenceId.HasValue)
            .ToList();

        foreach (var item in renewalItems)
        {
            _logger.LogInformation(
                "Processing CargoDry renewal for KitId={KitId} from Order={OrderId}",
                item.ReferenceId, message.OrderId);

            await _mediator.Send(new RenewKitCommand
            {
                KitId      = item.ReferenceId!.Value,
                AddedDays  = item.Quantity * 90,  // Product-based days — refine as needed
                Type       = RenewalType.OnlinePurchase,
                PaymentRef = message.OrderId.ToString(),
            }, ct);
        }
    }

    protected override Task ExecuteRollbackMessage(
        CommerceOrderCompletedMessage message, CancellationToken ct)
    {
        _logger.LogError(
            "CommerceOrderCompletedConsumer rollback for OrderId={OrderId}", message.OrderId);
        return Task.CompletedTask;
    }
}
```

> **Not:** `CommerceOrderCompletedMessage` Commerce modülü Abstraction'ına eklenmeli.
> Eğer yoksa geçici stub: `Message/CommerceOrderCompletedMessage.cs` with `long OrderId`, `List<OrderItemMessage> Items`.

---

## STEP 6 — DependencyInjection

**`DependencyInjection.cs`**
```csharp
using Aizen.Modules.CargoDry.Abstraction.Interface.Service;
using Aizen.Modules.CargoDry.Application.Jobs;
using Aizen.Modules.CargoDry.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Aizen.Modules.CargoDry.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddCargoDryApplication(this IServiceCollection services)
    {
        services.AddScoped<ICargoDryQrService,      CargoDryQrService>();
        services.AddScoped<IActivationTokenService, ActivationTokenService>();
        services.AddScoped<IBatchKeyVaultService,   BatchKeyVaultService>();

        services.AddHostedService<KitExpiryReminderJob>();
        services.AddHostedService<KitExpiredMarkingJob>();

        return services;
    }
}
```

---

## STEP 7 — csproj References

**`Aizen.Modules.CargoDry.Application.csproj`**:
```xml
<PackageReference Include="QRCoder" Version="1.*" />
<PackageReference Include="System.IdentityModel.Tokens.Jwt" Version="8.*" />
<PackageReference Include="Microsoft.IdentityModel.Tokens" Version="8.*" />
<PackageReference Include="FluentValidation" Version="11.*" />
<PackageReference Include="StackExchange.Redis" Version="2.*" />
<PackageReference Include="MediatR" Version="12.*" />
<PackageReference Include="MassTransit" Version="8.*" />
<ProjectReference Include="..\Aizen.Modules.CargoDry.Domain\..." />
<ProjectReference Include="..\Aizen.Modules.CargoDry.Abstraction\..." />
<ProjectReference Include="..\Aizen.Modules.CargoDry.Repository\..." />
```

## Verification Checklist

- [ ] `ValidateKitCommandHandler` — 6 katmanlı doğrulama, FixedTimeEquals kullanıldı
- [ ] `ActivateKitCommandHandler` — JTI token consume edildi, SaveChanges sonrası event publish
- [ ] `GenerateBatchCommandHandler` — Batch key üretildi, kit'ler batch'le eşleşti
- [ ] `KitExpiryReminderJob` — 30/7/1 gün kala çalışır, 09:00 UTC
- [ ] `KitExpiredMarkingJob` — saatlik, toplu MarkExpired + event
- [ ] `ActivationTokenService` — Redis Lua script ile atomic JTI consume
- [ ] `CargoDryQrService` — Base32 seri no, HMAC-SHA256 imza, FixedTimeEquals verify
