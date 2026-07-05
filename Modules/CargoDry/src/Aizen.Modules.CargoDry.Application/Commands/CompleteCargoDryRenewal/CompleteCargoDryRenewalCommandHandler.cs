using Aizen.Core.CQRS.Handler;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Core.InfoAccessor.Abstraction;
using Aizen.Modules.CargoDry.Abstraction.Dto;
using Aizen.Modules.CargoDry.Abstraction.Enum;
using Aizen.Modules.CargoDry.Application.Commands.PrepareCargoDryKitRenewal;
using Aizen.Modules.CargoDry.Application.Commands.RenewKit;
using Aizen.Modules.CargoDry.Domain.Interface.Repository;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Aizen.Modules.CargoDry.Application.Commands.CompleteCargoDryRenewal;

[DocumentationInfo("CompleteCargoDryRenewalCommandHandler",
    "Admin-explicit completion of a renewal preparation. " +
    "Calls the existing RenewKitCommand with RenewalType.AdminExtension. " +
    "Marks the preparation as Completed. " +
    "Hard rule #8: renewal must require explicit admin/payment/manual confirmation. " +
    "Hard rule #14: failed notification delivery does NOT block completion. " +
    "Phase 11 (July 2026).")]
public sealed class CompleteCargoDryRenewalCommandHandler
    : AizenCommandHandler<CompleteCargoDryRenewalCommand, CargoDryRenewalPreparationDto>
{
    private readonly ICargoDryRenewalPreparationRepository _preparations;
    private readonly ICargoDryProductRepository            _products;
    private readonly ISender                               _sender;
    private readonly IAizenInfoAccessor                    _info;
    private readonly ILogger<CompleteCargoDryRenewalCommandHandler> _logger;

    public CompleteCargoDryRenewalCommandHandler(
        ICargoDryRenewalPreparationRepository              preparations,
        ICargoDryProductRepository                         products,
        ISender                                            sender,
        IAizenInfoAccessor                                 info,
        ILogger<CompleteCargoDryRenewalCommandHandler>     logger)
    {
        _preparations = preparations;
        _products     = products;
        _sender       = sender;
        _info         = info;
        _logger       = logger;
    }

    public override async Task<CargoDryRenewalPreparationDto> Handle(
        CompleteCargoDryRenewalCommand request, CancellationToken ct)
    {
        var preparation = await _preparations.GetByIdAsync(request.RenewalPreparationId, ct)
            ?? throw new AizenBusinessException(
                $"Renewal preparation {request.RenewalPreparationId} not found.");

        // ── Idempotency ────────────────────────────────────────────────────────
        if (preparation.Status == CargoDryRenewalPreparationStatus.Completed)
        {
            _logger.LogInformation(
                "Renewal preparation {Id} ({Code}) already completed. Returning.",
                preparation.Id, preparation.RenewalCode);
            var productForIdem = await _products.GetByCodeAsync(preparation.ProductCode, ct);
            return PrepareCargoDryKitRenewalCommandHandler.MapToDto(preparation, productForIdem?.Name);
        }

        // ── State guards ───────────────────────────────────────────────────────
        if (preparation.Status == CargoDryRenewalPreparationStatus.Cancelled)
            throw new AizenBusinessException(
                $"Renewal preparation {preparation.Id} ({preparation.RenewalCode}) is already cancelled.");

        if (preparation.Status == CargoDryRenewalPreparationStatus.Failed)
            throw new AizenBusinessException(
                $"Renewal preparation {preparation.Id} ({preparation.RenewalCode}) is in Failed status. " +
                "Create a new preparation instead.");

        // ── CanComplete check ─────────────────────────────────────────────────
        // Requires either InvoiceId or ManualPaymentReference — hard rule #8
        var effectiveRef = request.ManualPaymentReference
                        ?? preparation.ManualPaymentReference;

        if (!preparation.InvoiceId.HasValue && string.IsNullOrWhiteSpace(effectiveRef))
            throw new AizenBusinessException(
                $"Renewal preparation {preparation.Id} ({preparation.RenewalCode}) cannot be completed: " +
                "requires either an invoice (PrepareRenewalInvoice) or a ManualPaymentReference. " +
                "Hard rule #8: renewal completion must require explicit admin/payment confirmation.");

        // ── Delegate actual kit renewal to existing RenewKitCommand ───────────
        var renewalDays     = preparation.RequestedRenewalMonths * 30;
        var completedByUserId = _info.UserInfoAccessor.UserInfo.UserId;

        await _sender.Send(new RenewKitCommand
        {
            KitId      = preparation.KitId,
            AddedDays  = renewalDays,
            Type       = RenewalType.AdminExtension,
            PaymentRef = effectiveRef ?? $"PREP-{preparation.RenewalCode}",
        }, ct);

        // ── Mark preparation completed ─────────────────────────────────────────
        preparation.Complete(
            completedByUserId:      completedByUserId,
            manualPaymentReference: effectiveRef,
            note:                   request.Note);

        await _preparations.SaveChangesAsync(ct);

        _logger.LogInformation(
            "CargoDry renewal completed. KitId={KitId} KitCode={KitCode} " +
            "PreparationId={PrepId} Code={Code} CompletedBy={UserId}",
            preparation.KitId, preparation.KitCode,
            preparation.Id, preparation.RenewalCode, completedByUserId);

        var product = await _products.GetByCodeAsync(preparation.ProductCode, ct);
        return PrepareCargoDryKitRenewalCommandHandler.MapToDto(preparation, product?.Name);
    }
}
