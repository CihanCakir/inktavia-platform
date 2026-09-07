using Aizen.Bff.MarineProvider.Application.Contracts.Reference;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.MarineProvider.Application.Reference;

/// <summary>GET /api/v1/provider/reference/countries — country options (name + dial code).</summary>
public sealed class GetCountriesQuery : AizenQuery<List<CountryItemDto>>
{
}
