using Aizen.Core.CQRS.Handler;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.Payment.Domain.Interface.Repository;
using Microsoft.Extensions.Logging;

namespace Aizen.Modules.Payment.Application.Commands.UpdatePartCommercialTerm;

[DocumentationInfo("UpdatePartCommercialTermCommandHandler",
    "Admin updates a part commercial term's money/dates. The domain Update re-validates coherence; the scope/priority overlap " +
    "guard re-runs (self-excluded). Scope and version are immutable. Cost is never logged.")]
public sealed class UpdatePartCommercialTermCommandHandler
    : AizenCommandHandler<UpdatePartCommercialTermCommand, UpdatePartCommercialTermResult>
{
    private readonly IPartCommercialTermRepository _terms;
    private readonly ILogger<UpdatePartCommercialTermCommandHandler> _logger;

    public UpdatePartCommercialTermCommandHandler(
        IPartCommercialTermRepository terms, ILogger<UpdatePartCommercialTermCommandHandler> logger)
    { _terms = terms; _logger = logger; }

    public override async Task<UpdatePartCommercialTermResult?> Handle(
        UpdatePartCommercialTermCommand request, CancellationToken ct)
    {
        var term = await _terms.GetByIdAsync(request.Id, ct)
            ?? throw new AizenBusinessException(
                (int)PaymentErrorCode.PartCommercialTermNotFound, $"Part commercial term {request.Id} not found.");

        term.Update(
            request.SupplierListPrice, request.ProviderDealerMargin,
            request.MaxCustomerDiscount, request.SupplierFundedAmount, request.ProviderFundedAmount, request.PlatformFundedAmount,
            request.MinimumProviderReceivable, request.MaximumDiscountableAmount,
            request.Priority, request.EffectiveFrom.ToUniversalTime(), request.EffectiveTo?.ToUniversalTime(),
            request.TermName, request.Notes);

        var conflict = await _terms.FindOverlappingActiveTermAsync(term, ct);
        if (conflict is not null)
            throw new AizenBusinessException(
                (int)PaymentErrorCode.PartCommercialTermConflict,
                $"The updated range conflicts with an existing active part commercial term (Id={conflict.Id}, Code={conflict.TermCode}).");

        _terms.Update(term);
        _logger.LogInformation("Part commercial term updated. Id={Id} Code={Code}", term.Id, term.TermCode);
        return new UpdatePartCommercialTermResult(term.Id, term.TermCode);
    }
}
