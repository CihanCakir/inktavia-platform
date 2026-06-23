using Aizen.Core.Api.Middleware;
using Aizen.Core.Domain;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.Identity.Abstraction;
using Aizen.Modules.Payment.Abstraction;

namespace Aizen.Modules.Identity.Domain.Entities
{
    public class UserProfileEntity : AizenEntityWithAudit
    {
        public long UserId { get; set; }
        public virtual UserEntity User { get; set; } = null!;
        public TaxpayerType TaxpayerType { get; private set; }
        public WorkshopRoleContext RoleContext { get; set; }

        public string FirstName { get; private set; } = null!;
        public string LastName { get; private set; } = null!;
        public string? Gender { get; private set; }
        public DateTime? BirthDate { get; private set; }


        public string? Bio { get; private set; }
        public string? ProfilePhotoUrl { get; private set; }
        public string? NationalityId { get; set; }


        public ApprovalStatus ApprovalStatus { get; private set; } = ApprovalStatus.Pending;
        public ProfileStatus Status { get; private set; } = ProfileStatus.Inactive;
        public DateTime? ApprovedAt { get; private set; }
        public DateTime? RejectedAt { get; private set; }
        public string? RejectReason { get; private set; }


        public long? PaymentProfileId { get; set; }

        public string? CompanyName { get; private set; }
        public string? City { get; private set; }
        public string? Country { get; private set; }
        public string? RejectionCategory { get; private set; }
        public string? InternalNote { get; private set; }
        public string? ReviewedBy { get; private set; }
        public DateTime? ReviewedAt { get; private set; }

        public virtual ICollection<VerificationDocumentEntity> VerificationDocuments { get; private set; }
            = new List<VerificationDocumentEntity>();

        public virtual ICollection<RiskSignalEntity> RiskSignals { get; private set; }
            = new List<RiskSignalEntity>();

        // ------------------------
        // Factory Method (Creation)
        // ------------------------

        public static UserProfileEntity Create(
            long userId,
            string firstName,
            string lastName,
            TaxpayerType taxpayerType,
            string? gender = null,
            DateTime? birthDate = null,
            string? bio = null,
            string? profilePhotoUrl = null)
        {
            return new UserProfileEntity
            {
                UserId = userId,
                FirstName = firstName,
                LastName = lastName,
                TaxpayerType = taxpayerType,
                Gender = gender,
                BirthDate = birthDate,
                Bio = bio,
                ProfilePhotoUrl = profilePhotoUrl,
                CreateDate = DateTime.UtcNow,
                ModifyDate = DateTime.UtcNow,
            };
        }


        // ---- Davranışlar ----
        public void Approve(string? reviewedBy = null)
        {
            if (ApprovalStatus == ApprovalStatus.Approved)
                return; // idempotent

            if (ApprovalStatus == ApprovalStatus.Rejected)
                throw new AizenBusinessException(((int)AizenErrorCode.ProfileAlreadyRejected).ToString());

            ApprovalStatus = ApprovalStatus.Approved;
            ApprovedAt = DateTime.UtcNow;
            ReviewedBy = reviewedBy;
            ReviewedAt = DateTime.UtcNow;
            RejectReason = null;
            RejectedAt = null;
            Touch();
        }

        public void Reject(string reason, string? reasonCategory = null, string? internalNote = null, string? reviewedBy = null)
        {
            if (ApprovalStatus == ApprovalStatus.Rejected)
                return; // idempotent

            if (ApprovalStatus == ApprovalStatus.Approved)
                throw new AizenBusinessException(((int)AizenErrorCode.ProfileAlreadyApproved).ToString());

            if (string.IsNullOrWhiteSpace(reason))
                throw new AizenBusinessException(((int)AizenErrorCode.RejectReasonRequired).ToString());

            ApprovalStatus = ApprovalStatus.Rejected;
            RejectedAt = DateTime.UtcNow;
            RejectReason = reason.Trim();
            RejectionCategory = reasonCategory;
            InternalNote = internalNote;
            ReviewedBy = reviewedBy;
            ReviewedAt = DateTime.UtcNow;
            Touch();
        }

        public void AddVerificationDocument(VerificationDocumentEntity document)
        {
            VerificationDocuments.Add(document);
        }

        public void SetCompanyName(string? companyName)
        {
            CompanyName = companyName;
            SetModified();
        }

        public void SetLocation(string? city, string? country)
        {
            City = city;
            Country = country;
            SetModified();
        }

        private void Touch() => ModifyDate = DateTime.UtcNow;
        // ------------------------
        // Behavior Methods
        // ------------------------

        public void ChangeName(string firstName, string lastName)
        {
            FirstName = firstName;
            LastName = lastName;
            SetModified();
        }

        public void UpdateTaxpayerType(TaxpayerType newType)
        {
            if (TaxpayerType != newType)
            {
                TaxpayerType = newType;
                SetModified();
            }
        }

        public void UpdateBio(string? bio)
        {
            Bio = bio;
            SetModified();
        }

        public void UpdateProfilePhoto(string? photoUrl)
        {
            ProfilePhotoUrl = photoUrl;
            SetModified();
        }

        public void UpdateGender(string? gender)
        {
            Gender = gender;
            SetModified();
        }

        public void UpdateBirthDate(DateTime? birthDate)
        {
            BirthDate = birthDate;
            SetModified();
        }

        private void SetModified()
        {
            ModifyDate = DateTime.UtcNow;
        }


        public string Name() => FirstName;



    }
}
