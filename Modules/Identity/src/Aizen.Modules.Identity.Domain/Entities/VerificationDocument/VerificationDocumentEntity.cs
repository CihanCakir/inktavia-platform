using Aizen.Core.Domain;

namespace Aizen.Modules.Identity.Domain.Entities
{
    public class VerificationDocumentEntity : AizenEntityWithAudit
    {
        public long ProfileId { get; private set; }

        public long FileId { get; private set; }

        /// <summary>
        /// Public identifier for the file (from FileStorage). Used for external API references.
        /// The long FileId is kept for legacy/internal use; this Guid is the canonical external id.
        /// </summary>
        public Guid? FilePublicId { get; private set; }

        public string Name { get; private set; } = string.Empty;

        public string DocumentType { get; private set; } = string.Empty;

        public string? Format { get; private set; }

        public string? FileSizeDisplay { get; private set; }

        public string? ContentType { get; private set; }

        public long SizeInBytes { get; private set; }

        public string? Issuer { get; private set; }

        public string? MatchScore { get; private set; }

        public long UploadedByUserId { get; private set; }

        public DateTime UploadedAt { get; private set; }

        // EF Core lazy-loading proxies (Castle DynamicProxy) subclass the entity, so the parameterless ctor must be
        // at least protected. A private one makes every query that materializes this type fail at runtime.
        protected VerificationDocumentEntity() { }

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

        /// <summary>
        /// Creates a verification document from a BFF attach-document request.
        /// Uses Guid filePublicId as the canonical file reference.
        /// </summary>
        public static VerificationDocumentEntity CreateFromBff(
            long profileId,
            Guid filePublicId,
            string documentType,
            string? issuer,
            string? name = null,
            string? contentType = null,
            long sizeInBytes = 0,
            long uploadedByUserId = 0)
        {
            return new VerificationDocumentEntity
            {
                ProfileId = profileId,
                FileId = 0,
                FilePublicId = filePublicId,
                PublicId = Guid.NewGuid(),
                Name = name ?? documentType,
                DocumentType = documentType,
                ContentType = contentType,
                SizeInBytes = sizeInBytes,
                Format = contentType,
                FileSizeDisplay = sizeInBytes > 0 ? $"{sizeInBytes / 1024.0:F0} KB" : null,
                Issuer = issuer,
                UploadedByUserId = uploadedByUserId,
                UploadedAt = DateTime.UtcNow,
                CreateDate = DateTime.UtcNow,
                ModifyDate = DateTime.UtcNow,
            };
        }

        /// <summary>Soft-delete the document.</summary>
        public void MarkDeleted()
        {
            IsDeleted = true;
            ModifyDate = DateTime.UtcNow;
        }
    }
}
