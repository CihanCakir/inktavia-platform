using Aizen.Core.Domain;

namespace Aizen.Modules.Identity.Domain.Entities
{
    public class VerificationDocumentEntity : AizenEntityWithAudit
    {
        public long ProfileId { get; private set; }

        public long FileId { get; private set; }

        public string Name { get; private set; } = string.Empty;

        public string DocumentType { get; private set; } = string.Empty;

        public string? Format { get; private set; }

        public string? FileSizeDisplay { get; private set; }

        public string? Issuer { get; private set; }

        public string? MatchScore { get; private set; }

        public long UploadedByUserId { get; private set; }

        public DateTime UploadedAt { get; private set; }

        private VerificationDocumentEntity() { }

        public static VerificationDocumentEntity Create(
            long profileId,
            long fileId,
            string name,
            string documentType,
            string? format,
            string? fileSizeDisplay,
            string? issuer,
            long uploadedByUserId)
        {
            return new VerificationDocumentEntity
            {
                ProfileId = profileId,
                FileId = fileId,
                Name = name,
                DocumentType = documentType,
                Format = format,
                FileSizeDisplay = fileSizeDisplay,
                Issuer = issuer,
                UploadedByUserId = uploadedByUserId,
                UploadedAt = DateTime.UtcNow,
                CreateDate = DateTime.UtcNow,
                ModifyDate = DateTime.UtcNow,
            };
        }
    }
}
