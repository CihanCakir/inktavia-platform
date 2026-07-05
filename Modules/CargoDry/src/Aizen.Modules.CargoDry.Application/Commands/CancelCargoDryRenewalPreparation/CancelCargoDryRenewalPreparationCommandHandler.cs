using Aizen.Core.CQRS.Handler;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Core.InfoAccessor.Abstraction;
using Aizen.Modules.CargoDry.Abstraction.Dto;
using Aizen.Modules.CargoDry.Abstraction.Enum;
using Aizen.Modules.CargoDry.Application.Commands.PrepareCargoDryKitRenewal;
using Aizen.Modules.CargoDry.Domain.Interface.Repository;
using Microsoft.Extensions.Logging;

namespace Aizen.Modules.CargoDry.Application.Commands.CancelCargoDryRenewalPreparation;

[DocumentationInfo("CancelCargoDryRenewalPreparationCommandHandler",
    "Cancels an open renewal preparation. Idempotent if already cancelled. " +
    "Cannot cancel a Completed preparation. " +
    "Phase 11 (July 2026).")]
public sealed class CancelCargoDryRenewalPreparationCommandHandler
    : AizenCommandHandler<CancelCargoDryRenewalPreparationCommand, CargoDryRenewalPreparationDto>
{
    private readonly ICargoDryRenewalPreparationRepository _preparations;
    private readonly ICargoDryProductRepository            _products;
    private readonly IAizenInfoAccessor                    _info;
    private readonly ILogger<CancelCargoDryRenewalPreparationCommandHandler> _logger;

    public CancelCargoDryRenewalPreparationCommandHandler(
        ICargoDryRenewalPreparationRepository                   preparations,
        ICargoDryProductRepository                              products,
        IAizenInfoAccessor                                      info,
        ILogger<CancelCargoDryRenewalPreparationCommandHandler> logger)
    {
        _preparations = preparations;
        _products     = products;
        _info         = info;
        _logger       = logger;
    }

    public override async Task<CargoDryRenewalPreparationDto> Handle(
        CancelCargoDryRenewalPreparationCommand request, CancellationToken ct)
    {
        var preparation = await _preparations.GetByIdAsync(request.RenewalPreparationId, ct)
            ?? throw new AizenBusinessException(
                $"Renewal preparation {request.RenewalPreparationId} not found.");

        if (preparation.Status == CargoDryRenewalPreparationStatus.Completed)
            throw new AizenBusinessException(
                $"Renewal preparation {preparation.Id} ({preparation.RenewalCode}) is already completed " +
                "and cannot be cancelled.");

        if (preparation.Status == CargoDryRenewalPreparationStatus.Cancelled)
        {
            _logger.LogInformation(
                "Renewal preparation {Id} ({Code}) already cancelled. Returning.",
                preparation.Id, preparation.RenewalCode);
            var productForIdem = await _products.GetByCodeAsync(preparation.ProductCode, ct);
            return PrepareCargoDryKitRenewalCommandHandler.MapToDto(preparation, productForIdem?.Name);
        }

        preparation.Cancel(
            cancelledByUserId:  _info.UserInfoAccessor.UserInfo.UserId,
            cancellationReason: request.CancellationReason,
            note:               request.Note);

        await _preparations.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Renewal preparation cancelled. PreparationId={Id} Code={Code} Reason={Reason}",
            preparation.Id, preparation.RenewalCode, request.CancellationReason);

        var product = await _products.GetByCodeAsync(preparation.ProductCode, ct);
        return PrepareCargoDryKitRenewalCommandHandler.MapToDto(preparation, product?.Name);
    }
}
