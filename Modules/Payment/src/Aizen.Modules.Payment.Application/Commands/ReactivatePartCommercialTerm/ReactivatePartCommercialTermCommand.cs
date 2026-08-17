using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.Payment.Domain.Interface.Repository;
using Microsoft.Extensions.Logging;

namespace Aizen.Modules.Payment.Application.Commands.ReactivatePartCommercialTerm;

/// <summary>BE-S5a — admin reactivates a previously deactivated (Inactive) part commercial term. Re-runs the overlap guard.</summary>
public sealed class ReactivatePartCommercialTermCommand : AizenCommand<bool>
{
    public required long Id { get; init; }
}

[DocumentationInfo("ReactivatePartCommercialTermCommandHandler",
    "Admin reactivates an Inactive part commercial term. Only an Inactive term may be reactivated; the scope/priority overlap " +
    "guard re-runs so a reactivation cannot resurrect a conflict.")]
public sealed class ReactivatePartCommercialTermCommandHandler
    : AizenCommandHandler<ReactivatePartCommercialTermCommand, bool>
{
    private readonly IPartCommercialTermRepository _terms;
    private readonly ILogger<ReactivatePartCommercialTermCommandHandler> _logger;

    public ReactivatePartCommercialTermCommandHandler(
        IPartCommercialTermRepository terms, ILogger<ReactivatePartCommercialTermCommandHandler> logger)
    { _terms = terms; _logger = logger; }

    public override async Task<bool> Handle(ReactivatePartCommercialTermCommand request, CancellationToken ct)
    {
        var term = await _terms.GetByIdAsync(request.Id, ct)
            ?? throw new AizenBusinessException(
                (int)PaymentErrorCode.PartCommercialTermNotFound, $"Part commercial term {request.Id} not found.");

        if (term.IsActive)
            throw new AizenBusinessException(
                (int)PaymentErrorCode.PartCommercialTermNotInactive,
                $"Part commercial term {request.Id} is not Inactive; only an Inactive term can be reactivated.");

        term.Reactivate();

        var conflict = await _terms.FindOverlappingActiveTermAsync(term, ct);
        if (conflict is not null)
            throw new AizenBusinessException(
                (int)PaymentErrorCode.PartCommercialTermConflict,
                $"Reactivation conflicts with an existing active part commercial term (Id={conflict.Id}, Code={conflict.TermCode}).");

        _terms.Update(term);
        _logger.LogInformation("Part commercial term reactivated. Id={Id} Code={Code}", term.Id, term.TermCode);
        return true;
    }
}
