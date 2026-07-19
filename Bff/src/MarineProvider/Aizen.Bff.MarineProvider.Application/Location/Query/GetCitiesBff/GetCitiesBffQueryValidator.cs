using Aizen.Core.Validation;
using FluentValidation;

namespace Aizen.Bff.MarineProvider.Application.Location;

public sealed class GetCitiesBffQueryValidator : AizenValidator<GetCitiesBffQuery>
{
    public GetCitiesBffQueryValidator()
    {
        RuleFor(x => x.Country).NotEmpty();
    }
}
