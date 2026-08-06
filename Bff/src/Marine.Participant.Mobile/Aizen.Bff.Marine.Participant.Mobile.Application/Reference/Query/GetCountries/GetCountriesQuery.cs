using Aizen.Bff.Marine.Participant.Mobile.Application.Contracts.Reference;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.Marine.Participant.Mobile.Application.Reference;

/// <summary>GET /api/v1/mobile/reference/countries — country options (name + dial code).</summary>
public sealed class GetCountriesQuery : AizenQuery<List<CountryItemDto>>
{
}
