using Aizen.Core.CQRS.Handler;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.Payment.Domain.Entities.PartCommercialTerm;
using Aizen.Modules.Payment.Domain.Interface.Repository;
using Microsoft.Extensions.Logging;

namespace Aizen.Modules.Payment.Application.Commands.CreatePartCommercialTerm;

[DocumentationInfo("CreatePartCommercialTermCommandHandler",
    "Admin creates a part commercial term. The domain factory validates money coherence (all ≥ 0, Σfunded ≤ maxDiscountable, " +
    "maxCustomerDiscount ≤ maxDiscountable); the create appends a NEW version (never mutates an active row) after the " +
    "scope/priority overlap guard. Cost values are never logged.")]
public sealed class CreatePartCommercialTermCommandHandler
    : AizenCommandHandler<CreatePartCommercialTermCommand, CreatePartCommercialTermResult>
{
    private readonly IPartCommercialTermRepository _terms;
    private readonly ILogger<CreatePartCommercialTermCommandHandler> _logger;

    public CreatePartCommercialTermCommandHandler(
        IPartCommercialTermRepository terms, ILogger<CreatePartCommercialTermCommandHandler> logger)
    { _terms = terms; _logger = logger; }

    public override async Task<CreatePartCommercialTermResult?> Handle(
        CreatePartCommercialTermCommand request, CancellationToken ct)
    {
        var termCode = await _terms.GenerateCodeAsync(ct);

        var term = PartCommercialTermEntity.Create(
            request.Brand, request.ProductCode, request.ProviderProfileId, request.CategoryCode, request.CurrencyCode,
            request.SupplierListPrice, request.ProviderDealerMargin,
            request.MaxCustomerDiscount, request.SupplierFundedAmount, request.ProviderFundedAmount, request.PlatformFundedAmount,
            request.MinimumProviderReceivable, request.MaximumDiscountableAmount,
            version: 1, priority: request.Priority,
            effectiveFrom: request.EffectiveFrom.ToUniversalTime(), effectiveTo: request.EffectiveTo?.ToUniversalTime(),
            termCode: termCode, termName: request.TermName, notes: request.Notes);

        // Append-a-new-version: the version is (max existing version for the exact scope) + 1.
        var nextVersion = await _terms.GetMaxVersionForScopeAsync(term, ct) + 1;
        term.SetVersion(nextVersion);

        var conflict = await _terms.FindOverlappingActiveTermAsync(term, ct);
        if (conflict is not null)
            throw new AizenBusinessException(
                (int)PaymentErrorCode.PartCommercialTermConflict,
                $"A conflicting active part commercial term already exists (Id={conflict.Id}, Code={conflict.TermCode}).");

        await _terms.AddAsync(term, ct);
        // SaveChanges handled by AizenCommandHandlerDecorator.

        // NOTE: cost/margin are deliberately NOT logged (§20.9 confidentiality).
        _logger.LogInformation(
            "Part commercial term created. Code={Code} Version={Version} Scope(brand={Brand},product={Product},provider={Provider},category={Category})",
            termCode, nextVersion, term.Brand, term.ProductCode, term.ProviderProfileId, term.CategoryCode);
        return new CreatePartCommercialTermResult(term.Id, termCode, nextVersion);
    }
}
