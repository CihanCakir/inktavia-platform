using Aizen.Bff.MarineProvider.Application.Common.Services;
using Aizen.Bff.MarineProvider.Application.Contracts.Me;
using Aizen.Core.CQRS.Handler;

namespace Aizen.Bff.MarineProvider.Application.Me.GetProviderMe;

public sealed class GetProviderMeQueryHandler
    : AizenQueryHandler<GetProviderMeQuery, GetProviderMeResponse>
{
    private readonly IProviderContext _context;

    public GetProviderMeQueryHandler(IProviderContext context) => _context = context;

    public override Task<GetProviderMeResponse?> Handle(
        GetProviderMeQuery request, CancellationToken cancellationToken)
    {
        var response = new GetProviderMeResponse
        {
            KeycloakSubject = _context.KeycloakSubject,
            Email = _context.Email,
            PreferredUsername = _context.PreferredUsername,
            FirstName = _context.FirstName,
            LastName = _context.LastName,
            EmailVerified = _context.EmailVerified,
            ProviderProfileId = _context.ProviderProfileId,
            HasProfileLink = _context.HasProfileLink,
            Roles = _context.Roles
        };

        return Task.FromResult<GetProviderMeResponse?>(response);
    }
}
