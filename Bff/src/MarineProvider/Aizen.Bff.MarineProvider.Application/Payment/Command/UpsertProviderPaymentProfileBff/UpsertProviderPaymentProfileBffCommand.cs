using Aizen.Core.CQRS.Message;
using Aizen.Modules.Payment.Abstraction.Dto;

namespace Aizen.Bff.MarineProvider.Application.Payment;

public sealed class UpsertProviderPaymentProfileBffCommand : AizenCommand<ProviderPaymentProfileDto>
{
    public string  Iban      { get; init; } = default!;
    public string? LegalName { get; init; }
    public string? TaxNumber { get; init; }
}
