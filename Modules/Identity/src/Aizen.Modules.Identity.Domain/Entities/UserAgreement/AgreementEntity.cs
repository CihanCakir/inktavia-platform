using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Aizen.Core.Domain;

namespace Aizen.Modules.Identity.Domain.Entities.UserAgreement
{
    public class AgreementEntity : AizenEntityWithAudit
    {
        public string Name { get; private set; } = null!;
        public decimal LastVersionNumber { get; private set; }
        public bool IsOptional { get; private set; }
        public string AgreementType { get; private set; } = null!;

        public virtual ICollection<UserAgreementEntity> UserAgreements { get; private set; } = new List<UserAgreementEntity>();

        // 📌 Domain Constructor
        protected AgreementEntity() { }

        public AgreementEntity(string name, decimal initialVersion, string agreementType, bool isOptional)
        {
            Name = name;
            LastVersionNumber = initialVersion;
            AgreementType = agreementType;
            IsOptional = isOptional;
            CreateDate = DateTime.UtcNow;
            ModifyDate = DateTime.UtcNow;
        }


        // ---- Domain davranışları (reflection yerine bunları kullanacağız) ----
        public void UpdateMeta(string name, bool isOptional)
        {
            if (Name != name) Name = name;
            if (IsOptional != isOptional) IsOptional = isOptional;
            ModifyDate = DateTime.UtcNow;
        }

        public void BumpVersionIfHigher(decimal newVersion)
        {
            if (newVersion > LastVersionNumber)
            {
                LastVersionNumber = newVersion;
                ModifyDate = DateTime.UtcNow;
            }
        }

        /// <summary>
        /// Yeni bir versiyon oluşturur (örneğin yeni metin yüklendiğinde).
        /// </summary>
        public void IncrementVersion()
        {
            LastVersionNumber += 1;
            ModifyDate = DateTime.UtcNow;
        }

        /// <summary>
        /// Opsiyonel durumunu değiştirir.
        /// </summary>
        public void SetOptional(bool isOptional)
        {
            IsOptional = isOptional;
            ModifyDate = DateTime.UtcNow;
        }

        /// <summary>
        /// Sözleşmenin adını günceller.
        /// </summary>
        public void Rename(string newName)
        {
            Name = newName;
            ModifyDate = DateTime.UtcNow;
        }
    }

}