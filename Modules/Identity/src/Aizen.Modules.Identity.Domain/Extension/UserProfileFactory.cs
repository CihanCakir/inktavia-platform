using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Aizen.Modules.Identity.Abstraction;
using Aizen.Modules.Identity.Domain.Entities;
using Aizen.Modules.Payment.Abstraction;

namespace Aizen.Modules.Identity.Domain.Extension
{
    public static class UserProfileFactory
    {
        public static UserProfileEntity CreateParticipantProfile(
            string? fullName,
            TaxpayerType taxpayerType = TaxpayerType.Individual,
            string? gender = null,
            DateTime? birthDate = null,
            string? profilePhotoUrl = null)
        {
            var (first, last) = SplitName(fullName);

            var p = UserProfileEntity.Create(
                userId: 0, // AddProfile çağrısında UserId set edilecek
                firstName: first,
                lastName: last ?? string.Empty,
                taxpayerType: taxpayerType,
                gender: gender,
                birthDate: birthDate,
                bio: null,
                profilePhotoUrl: profilePhotoUrl
            );
            p.RoleContext = WorkshopRoleContext.Participant;
            return p;
        }

        private static (string first, string? last) SplitName(string? name)
        {
            if (string.IsNullOrWhiteSpace(name)) return ("", null);
            var parts = name.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 1) return (parts[0], null);
            return (parts[0], string.Join(' ', parts.Skip(1)));
        }
    }


}