using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Aizen.Modules.Identity.Domain.Entities;

namespace Aizen.Modules.Identity.Domain.Interface.Repository
{
    public interface IUserRepository
    {
        /// <summary>
        /// Telefon numarasına göre kullanıcıyı getirir.
        /// </summary>
        Task<UserEntity> GetUserByPhoneNumber(string phoneNumber, bool disableTracking = false);

        /// <summary>
        /// Telefon numarasına göre kullanıcıyı kontrol eder.
        /// </summary>
        Task<UserEntity?> CheckUserByPhoneNumber(string phoneNumber, bool disableTracking = false);
        /// <summary>
        /// Kullanıcının başarısız giriş sayısını arttırır, gerekiyorsa lockout uygular.
        /// </summary>
        Task BlockUser(UserEntity user);

        /// <summary>
        /// Giriş başarısız olduğunda login deneme sayısını arttırır.
        /// </summary>
        Task FailLogin(UserEntity user);
    }
}