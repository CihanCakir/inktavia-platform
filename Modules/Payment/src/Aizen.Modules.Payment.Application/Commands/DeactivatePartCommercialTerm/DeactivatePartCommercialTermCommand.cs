using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.Payment.Domain.Interface.Repository;
using Microsoft.Extensions.Logging;

namespace Aizen.Modules.Payment.Application.Commands.DeactivatePartCommercialTerm;

/// <summary>BE-S5a — admin deactivates a part commercial term (soft; resolution ignores inactive terms).</summary>
public sealed class DeactivatePartCommercialTermCommand : AizenCommand<bool>
{
    public required long Id { get; init; }
}

[DocumentationInfo("DeactivatePartCommercialTermCommandHandler", "Admin deactivates a part commercial term.")]
public sealed class DeactivatePartCommercialTermCommandHandler
    : AizenCommandHandler<DeactivatePartCommercialTermCommand, bool>
{
    private readonly IPartCommercialTermRepository _terms;
    private readonly ILogger<DeactivatePartCommercialTermCommandHandler> _logger;

    public DeactivatePartCommercialTermCommandHandler(
        IPartCommercialTermRepository terms, ILogger<DeactivatePartCommercialTermCommandHandler> logger)
    { _terms = terms; _logger = logger; }

    public override async Task<bool> Handle(DeactivatePartCommercialTermCommand request, CancellationToken ct)
    {
        var term = await _terms.GetByIdAsync(request.Id, ct)
            ?? throw new AizenBusinessException(
                (int)PaymentErrorCode.PartCommercialTermNotFound, $"Part commercial term {request.Id} not found.");

        term.Deactivate();
        _terms.Update(term);
        _logger.LogInformation("Part commercial term deactivated. Id={Id} Code={Code}", term.Id, term.TermCode);
        return true;
    }
}
