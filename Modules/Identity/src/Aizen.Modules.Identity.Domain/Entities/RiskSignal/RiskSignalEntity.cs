using Aizen.Core.Domain;

namespace Aizen.Modules.Identity.Domain.Entities
{
    public class RiskSignalEntity : AizenEntityWithAudit
    {
        public long ProfileId { get; private set; }

        /// <summary>"high" | "medium" | "low"</summary>
        public string Severity { get; private set; } = string.Empty;

        /// <summary>Short label. e.g. "Duplicate email detected"</summary>
        public string Title { get; private set; } = string.Empty;

        /// <summary>Detailed description for the reviewer.</summary>
        public string Description { get; private set; } = string.Empty;

        /// <summary>Signal source code. e.g. "DUPLICATE_EMAIL" | "MISSING_TAX_ID"</summary>
        public string SignalCode { get; private set; } = string.Empty;

        public DateTime DetectedAt { get; private set; }

        private RiskSignalEntity() { }

        public static RiskSignalEntity Create(
            long profileId,
            string severity,
            string title,
            string description,
            string signalCode)
        {
            return new RiskSignalEntity
            {
                ProfileId = profileId,
                Severity = severity,
                Title = title,
                Description = description,
                SignalCode = signalCode,
                DetectedAt = DateTime.UtcNow,
                CreateDate = DateTime.UtcNow,
                ModifyDate = DateTime.UtcNow,
            };
        }
    }
}
