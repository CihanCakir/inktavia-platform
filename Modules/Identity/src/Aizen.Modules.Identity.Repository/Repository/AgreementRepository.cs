using Aizen.Core.Api.Middleware;
using Aizen.Core.InfoAccessor.Abstraction;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.Identity.Domain.Entities.UserAgreement;
using Aizen.Modules.Identity.Domain.Interface.Repository;
using Aizen.Modules.Identity.Repository.Context;
using Microsoft.EntityFrameworkCore;

namespace Aizen.Modules.Identity.Repository.Identity.Repository
{
    public class AgreementRepository : IAgreementRepository
    {
        private readonly IdentityDbContext _context;
        private readonly IAizenInfoAccessor _aizenInfoAccessor;

        public AgreementRepository(IdentityDbContext context, IAizenInfoAccessor aizenInfoAccessor)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _aizenInfoAccessor = aizenInfoAccessor ?? throw new ArgumentNullException(nameof(aizenInfoAccessor));
        }

        public async Task<List<AgreementEntity>> GetUnapprovedAgreementsAsync(long userId, params string[] agreementTypes)
        {
            var agreements = await _context.Agreements
                .Where(x => agreementTypes.Contains(x.AgreementType))
                .ToListAsync();

            var result = new List<AgreementEntity>();

            foreach (var agreement in agreements)
            {
                bool isApproved = await _context.UserAgreements
                    .AnyAsync(x =>
                        x.UserId == userId &&
                        x.AgreementId == agreement.Id &&
                        x.VersionNumber == agreement.LastVersionNumber);

                if (!isApproved)
                    result.Add(agreement);
            }

            return result;
        }

        public async Task<bool> HasUserApprovedAgreementAsync(long userId, long agreementId)
        {
            var agreement = await _context.Agreements.FirstOrDefaultAsync(x => x.Id == agreementId);
            if (agreement is null)
                throw new AizenBusinessException(((int)AizenErrorCode.AgreementNotFound).ToString());

            return await _context.UserAgreements.AnyAsync(x =>
                x.UserId == userId &&
                x.AgreementId == agreementId &&
                x.VersionNumber == agreement.LastVersionNumber);
        }

        public async Task<bool> HasUserApprovedAllAsync(long userId, params string[] agreementTypes)
        {
            var unapproved = await GetUnapprovedAgreementsAsync(userId, agreementTypes);
            return !unapproved.Any();
        }

        public async Task<UserAgreementEntity> ApproveAgreementAsync(long userId, AgreementEntity agreement)
        {
            if (await HasUserApprovedAgreementAsync(userId, agreement.Id))
                throw new AizenBusinessException(((int)AizenErrorCode.AgreementAlreadyApproved).ToString());

            var userAgreement = UserAgreementEntity.ApproveAgreement(userId, agreement);

            await _context.UserAgreements.AddAsync(userAgreement);
            await _context.SaveChangesAsync();

            return userAgreement;
        }

        public async Task<List<AgreementEntity>> CheckAgreementsAsync(params string[] agrementTypes)
        {
            var result = new List<AgreementEntity>();
            var userId = Convert.ToInt64(_aizenInfoAccessor.UserInfoAccessor.UserInfo.UserId);

            var agreements = await _context.Agreements
                .AsNoTracking()
                .Where(x => agrementTypes.Contains(x.AgreementType))
                .ToListAsync();

            foreach (var agreement in agreements)
            {
                var isApproved = await _context.UserAgreements
                    .AsNoTracking()
                    .AnyAsync(x =>
                        x.UserId == userId &&
                        x.AgreementId == agreement.Id &&
                        x.VersionNumber == agreement.LastVersionNumber);

                if (!isApproved)
                    result.Add(agreement);
            }

            return result;
        }


        public Task<int> CheckAgreement(int id, int userId = 0)
        {
            throw new NotImplementedException();
        }

        public async Task<int> HasUserApprovedAgreementAsync(int agreementId, long userId)
        {
            var agreement = await _context.Agreements
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == agreementId);

            if (agreement == null)
                throw new AizenBusinessException("Agreement not found."); // veya özel hata koduyla

            var hasApproved = await _context.UserAgreements
                .AsNoTracking()
                .AnyAsync(x =>
                    x.UserId == userId &&
                    x.AgreementId == agreement.Id &&
                    x.VersionNumber == agreement.LastVersionNumber);

            return hasApproved ? agreementId : 0;
        }

    }

}