using Aizen.Core.Api.Middleware;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.Identity.Domain.Entities;
using Aizen.Modules.Identity.Domain.Interface.Repository;
using Aizen.Modules.Identity.Repository.Context;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
namespace Aizen.Modules.Identity.Repository.Identity.Repository
{
    public class UserRepository : IUserRepository
    {
        private readonly UserManager<UserEntity> _userManager;
        private readonly IdentityDbContext _dbContext;

        public UserRepository(UserManager<UserEntity> userManager, IdentityDbContext dbContext)
        {
            _userManager = userManager;
            _dbContext = dbContext;
        }

        public async Task<UserEntity> GetUserByPhoneNumber(string phoneNumber, bool disableTracking = false)
        {
            var query = _dbContext.Users.AsQueryable();

            if (disableTracking)
                query = query.AsNoTracking();

            return await query.FirstOrDefaultAsync(x => x.PhoneNumber == phoneNumber)
                   ?? throw new AizenBusinessException(((int)AizenErrorCode.UserNotFound).ToString());
        }

        public async Task<UserEntity?> CheckUserByPhoneNumber(string phoneNumber, bool disableTracking = false)
        {
            var query = _dbContext.Users.AsQueryable();

            if (disableTracking)
                query = query.AsNoTracking();

            return await query.FirstOrDefaultAsync(x => x.PhoneNumber == phoneNumber);
        }

        public async Task BlockUser(UserEntity user)
        {
            // Kullanıcıya lockout süresi uygula (örnek: 15 dakika)
            await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.UtcNow.AddMinutes(15));
            await _userManager.ResetAccessFailedCountAsync(user); // opsiyonel: sayaç sıfırlanabilir
        }

        public async Task FailLogin(UserEntity user)
        {
            // Giriş denemesi başarısız olduğunda kullanıcıya bir deneme ekle
            await _userManager.AccessFailedAsync(user);
        }
    }
}