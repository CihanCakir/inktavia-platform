using Aizen.Modules.Identity.Abstraction;
using Aizen.Modules.Identity.Domain.Entities;
using Aizen.Modules.Identity.Domain.Interface;
using Aizen.Modules.Identity.Repository.Context;
using Microsoft.EntityFrameworkCore;

namespace Aizen.Modules.Identity.Repository.Identity.Repository
{
    public class UserProfileRepository : IUserProfileRepository
    {
        private readonly IdentityDbContext _dbContext;

        public UserProfileRepository(IdentityDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<UserProfileEntity?> GetActiveProfileIdAsync(long userId, WorkshopRoleContext roleContext)
        {
            var profile = await _dbContext.UserProfiles
                .Where(p => p.UserId == userId && p.RoleContext == roleContext)
                .FirstOrDefaultAsync();
                return profile;
        }

        public async Task<bool> HasProfileForContextAsync(long userId, WorkshopRoleContext roleContext)
        {
            return await _dbContext.UserProfiles
                .AnyAsync(p => p.UserId == userId && p.RoleContext == roleContext);
        }

        public async Task<List<UserProfileEntity>> GetAllProfilesAsync(long userId)
        {
            return await _dbContext.UserProfiles
                .Where(p => p.UserId == userId)
                .ToListAsync();
        }

        public async Task<UserProfileEntity?> GetProfileByIdAsync(long profileId)
        {
            return await _dbContext.UserProfiles
                .FirstOrDefaultAsync(p => p.Id == profileId);
        }

        public async Task<UserProfileEntity?> GetOrganizerProfileByKeycloakSubjectAsync(
            string keycloakSubjectId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(keycloakSubjectId))
                return null;

            return await _dbContext.UserProfiles
                .AsNoTracking()
                .Include(p => p.User)
                .Include(p => p.VerificationDocuments)
                .Include(p => p.RiskSignals)
                .FirstOrDefaultAsync(
                    p => p.RoleContext == WorkshopRoleContext.Organizer
                         && !p.IsDeleted
                         && p.User.KeycloakSubjectId == keycloakSubjectId,
                    cancellationToken);
        }

        public async Task<UserProfileEntity?> GetParticipantProfileByKeycloakSubjectAsync(
            string keycloakSubjectId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(keycloakSubjectId))
                return null;

            return await _dbContext.UserProfiles
                .AsNoTracking()
                .Include(p => p.User)
                .Include(p => p.VerificationDocuments)
                .Include(p => p.RiskSignals)
                .FirstOrDefaultAsync(
                    p => p.RoleContext == WorkshopRoleContext.Participant
                         && !p.IsDeleted
                         && p.User.KeycloakSubjectId == keycloakSubjectId,
                    cancellationToken);
        }

        public async Task AddProfileAsync(UserProfileEntity profile)
        {
            await _dbContext.UserProfiles.AddAsync(profile);
        }

        public void UpdateProfileAsync(UserProfileEntity profile)
        {
            _dbContext.UserProfiles.Update(profile);
        }
    }

}