using Aizen.Core.CQRS.Message;
using Aizen.Modules.Payment.Abstraction.Dto;

namespace Aizen.Modules.Payment.Application.Commands.UpsertProviderPaymentProfile;

public sealed class UpsertProviderPaymentProfileCommand : AizenCommand<ProviderPaymentProfileDto>
{
    public long    ProviderProfileId { get; init; }
    public string  Iban              { get; init; } = default!;
    public string? LegalName         { get; init; }
    public string? TaxNumber         { get; init; }
}
