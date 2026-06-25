using Aizen.Core.CQRS.Handler;
using Aizen.Modules.CargoDry.Abstraction.Dto;
using Aizen.Modules.CargoDry.Abstraction.Interface.Service;
using Aizen.Modules.CargoDry.Domain.Entities;
using Aizen.Modules.CargoDry.Domain.Interface.Repository;
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

    public override async Task<GenerateBatchResultDto?> Handle(
        GenerateBatchCommand request, CancellationToken ct)
    {
        var product = await _products.GetByCodeAsync(request.ProductCode, ct)
            ?? throw new InvalidOperationException($"Product not found: {request.ProductCode}");

        var batchCode = $"{DateTime.UtcNow:yyyyMM}-{product.ProductCode[..Math.Min(4, product.ProductCode.Length)]}-{Guid.NewGuid().ToString("N")[..4].ToUpperInvariant()}";

        await _keyVault.CreateKeyAsync(batchCode, ct);

        var batch = CargoDryBatchEntity.Create(batchCode, product.ProductCode, request.Count, request.AdminUserId);
        await _batches.AddAsync(batch, ct);

        var kits = new List<CargoDryKitEntity>(request.Count);
        for (var i = 0; i < request.Count; i++)
        {
            var serial    = _qrService.GenerateSerialNumber();
            var kitCode   = $"CD-{serial[..4]}{serial[5..9]}";
            var sig       = await _qrService.SignAsync(serial, batchCode, ct);
            var qrPayload = $"https://app.inktavia.com/activate?s={Uri.EscapeDataString(serial)}&b={batchCode}&sig={Uri.EscapeDataString(sig)}";
            kits.Add(CargoDryKitEntity.Create(serial, kitCode, qrPayload, product.ProductCode, batchCode));
        }

        await _kits.AddRangeAsync(kits, ct);

        batch.SetFileRefs(
            $"cargodry/batches/{batchCode}/qr-codes.zip",
            $"cargodry/batches/{batchCode}/serial-list.csv");
        await _batches.SaveChangesAsync(ct);

        _logger.LogInformation("Generated batch {BatchCode}: {Count} kits", batchCode, request.Count);

        return new GenerateBatchResultDto
        {
            BatchCode      = batchCode,
            GeneratedCount = request.Count,
            QrZipFileUrl   = batch.QrZipFileRef!,
            ExcelFileUrl   = batch.ExcelFileRef!,
        };
    }
}
