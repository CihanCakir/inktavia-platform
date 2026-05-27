using Aizen.Core.InfoAccessor.Abstraction;
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
        private readonly IAizenInfoAccessor _infoAccessor;

        // Constructor injection for the DbContext
        public UserProfileRepository(IdentityDbContext dbContext, IAizenInfoAccessor infoAccessor)
        {
            _dbContext = dbContext;
            _infoAccessor = infoAccessor;
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
            var context = Enum.TryParse(_infoAccessor.AppInfoAccessor.AppInfo.Code, out WorkshopRoleContext rc)
                ? rc
                : WorkshopRoleContext.Participant;

            return await _dbContext.UserProfiles
                .FirstOrDefaultAsync(p => p.Id == profileId && p.RoleContext == context);
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