using Aizen.Bff.Marine.Participant.Mobile.Application.Common.Services;
using Aizen.Bff.Marine.Participant.Mobile.Application.Contracts.Me;
using Aizen.Core.CQRS.Handler;

namespace Aizen.Bff.Marine.Participant.Mobile.Application.Me;

public sealed class GetMeQueryHandler
    : AizenQueryHandler<GetMeQuery, MeResponse>
{
    private readonly IParticipantContext _context;

    public GetMeQueryHandler(IParticipantContext context) => _context = context;

    public override Task<MeResponse?> Handle(
        GetMeQuery request, CancellationToken cancellationToken)
    {
        var response = new MeResponse
        {
            Subject = _context.KeycloakSubject,
            Email = _context.Email,
            Username = _context.PreferredUsername,
            Roles = _context.Roles
        };

        return Task.FromResult<MeResponse?>(response);
    }
}
