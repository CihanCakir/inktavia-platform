using Aizen.Core.CQRS.Handler;
using Aizen.Core.UnitOfWork.Abstraction;
using Aizen.Modules.Identity.Abstraction;
using Aizen.Modules.Identity.Abstraction.Dto.Participant;
using Aizen.Modules.Identity.Domain.Entities;
using Aizen.Modules.Identity.Repository.Context;

namespace Aizen.Modules.InktaviaStore.Application.Identity.Query.Participant;

public sealed class GetParticipantProfileListQueryHandler
    : AizenQueryHandler<GetParticipantProfileListQuery, IList<ParticipantProfileListItemDto>>
{
    private readonly IAizenUnitOfWork<IdentityDbContext> _uow;

    public GetParticipantProfileListQueryHandler(IAizenUnitOfWork<IdentityDbContext> uow)
    {
        _uow = uow;
    }

    public override async Task<IList<ParticipantProfileListItemDto>> Handle(GetParticipantProfileListQuery request, CancellationToken cancellationToken)
    {
        var repo = _uow.GetRepository<UserProfileEntity>();

        var queryable = await repo.GetAllAsync<ParticipantProfileListItemDto>(
            selector: p => new ParticipantProfileListItemDto
            {
                Id = p.Id,
                UserId = p.UserId,
                FirstName = p.FirstName,
                LastName = p.LastName,
                ProfilePhotoUrl = p.ProfilePhotoUrl,
                ApprovalStatus = p.ApprovalStatus.ToString(),
                Status = p.Status.ToString(),
                CreateDate = p.CreateDate
            },
            predicate: p => p.RoleContext == WorkshopRoleContext.Participant && !p.IsDeleted
                && (request.FirstName == null || p.FirstName.Contains(request.FirstName))
                && (request.LastName == null || p.LastName.Contains(request.LastName))
                && (request.ApprovalStatus == null || p.ApprovalStatus.ToString() == request.ApprovalStatus));

        return queryable.ToList();
    }
}
