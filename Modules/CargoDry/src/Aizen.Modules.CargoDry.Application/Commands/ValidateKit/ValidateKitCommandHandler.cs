using Aizen.Core.CQRS.Handler;
using Aizen.Modules.CargoDry.Abstraction.Dto;
using Aizen.Modules.CargoDry.Abstraction.Enum;
using Aizen.Modules.CargoDry.Abstraction.Interface.Service;
using Aizen.Modules.CargoDry.Domain.Interface.Repository;

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

    public override async Task<CargoDryKitValidationDto?> Handle(
        ValidateKitCommand request, CancellationToken ct)
    {
        var batch = await _batches.GetByCodeAsync(request.BatchCode, ct);
        if (batch is null || batch.IsRevoked)
            return Invalid("BatchRevoked");

        if (!string.IsNullOrEmpty(request.Signature))
        {
            var sigOk = await _qrService.VerifyAsync(
                request.SerialNumber, request.BatchCode, request.Signature, ct);
            if (!sigOk) return Invalid("InvalidSignature");
        }

        var kit = await _kits.GetBySerialAsync(request.SerialNumber, ct);
        if (kit is null) return Invalid("KitNotFound");

        if (kit.Status != CargoDryKitStatus.Available)
            return Invalid(kit.Status == CargoDryKitStatus.Activated ? "AlreadyActivated" : "KitUnavailable");

        var product = await _products.GetByCodeAsync(kit.ProductCode, ct);
        if (product is null || !product.IsActive)
            return Invalid("ProductInactive");

        var token     = _tokenService.Generate(kit.SerialNumber);
        var expiresAt = DateTimeOffset.UtcNow.AddMinutes(5);

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
