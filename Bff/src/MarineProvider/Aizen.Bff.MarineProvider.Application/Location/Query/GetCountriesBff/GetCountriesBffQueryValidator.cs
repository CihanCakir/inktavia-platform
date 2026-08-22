using Aizen.Core.Validation;

namespace Aizen.Bff.MarineProvider.Application.Location;

// GetCitiesBff ile aynı şekil (query/validator/handler). Ülkeler için parametre yok → doğrulanacak alan yok.
public sealed class GetCountriesBffQueryValidator : AizenValidator<GetCountriesBffQuery>
{
    public GetCountriesBffQueryValidator()
    {
    }
}
