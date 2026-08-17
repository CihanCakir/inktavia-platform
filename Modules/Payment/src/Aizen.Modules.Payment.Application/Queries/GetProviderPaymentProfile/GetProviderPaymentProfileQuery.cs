using Aizen.Core.CQRS.Message;
using Aizen.Modules.Payment.Abstraction.Dto;

namespace Aizen.Modules.Payment.Application.Queries.GetProviderPaymentProfile;

public sealed class GetProviderPaymentProfileQuery : AizenQuery<ProviderPaymentProfileDto>
{
    public long ProviderProfileId { get; init; }
}
