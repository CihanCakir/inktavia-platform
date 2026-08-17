using Aizen.Bff.Marine.Participant.Mobile.Application.Common.RemoteClients;
using Aizen.Bff.Marine.Participant.Mobile.Application.Contracts.CargoDry;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.Infrastructure.Exception;
using Microsoft.Extensions.Logging;

namespace Aizen.Bff.Marine.Participant.Mobile.Application.CargoDry;

/// <summary>Validate a scanned kit. Required serial + batch are guarded here (clean 400) before the module call;
/// the module's own invalid/expired verdict comes back as <see cref="MobileKitValidationDto.IsValid"/> = false with
/// a reason (NOT an exception), so the app can show it inline. A transport failure (e.g. a missing audience mapper →
/// 401) surfaces as a clean business error, never an unhandled 500.</summary>
public sealed class ValidateMobileKitCommandHandler
    : AizenCommandHandler<ValidateMobileKitCommand, MobileKitValidationDto>
{
    private readonly ICargoDryRemoteCall _cargoDry;
    private readonly ILogger<ValidateMobileKitCommandHandler> _logger;

    public ValidateMobileKitCommandHandler(
        ICargoDryRemoteCall cargoDry, ILogger<ValidateMobileKitCommandHandler> logger)
    {
        _cargoDry = cargoDry;
        _logger = logger;
    }

    public override async Task<MobileKitValidationDto?> Handle(
        ValidateMobileKitCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.SerialNumber))
            throw new AizenBusinessException("Serial number is required.");
        if (string.IsNullOrWhiteSpace(request.BatchCode))
            throw new AizenBusinessException("Batch code is required.");

        try
        {
            var result = await _cargoDry.ValidateKit(new ValidateKitRemoteRequest
            {
                SerialNumber = request.SerialNumber.Trim(),
                BatchCode    = request.BatchCode.Trim(),
                Signature    = string.IsNullOrWhiteSpace(request.Signature) ? null : request.Signature!.Trim(),
            });

            if (result is null)
                throw new AizenBusinessException("Kit validation failed.");

            return MobileCargoDryMapper.MapValidation(result);
        }
        catch (Refit.ApiException ex)
        {
            _logger.LogWarning(ex, "CargoDry validate failed for serial {Serial} (status {Status}).",
                request.SerialNumber, ex.StatusCode);
            throw new AizenBusinessException("Kit validation failed.");
        }
    }
}
