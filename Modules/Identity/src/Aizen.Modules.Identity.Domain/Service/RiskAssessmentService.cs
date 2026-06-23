using Aizen.Modules.Identity.Domain.Entities;

namespace Aizen.Modules.Identity.Domain.Service
{
    /// <summary>
    /// Evaluates risk signals for a profile based on its current data.
    /// Called after profile is saved so that the profile.Id is populated.
    /// </summary>
    public static class RiskAssessmentService
    {
        public static IReadOnlyList<RiskSignalEntity> Evaluate(
            long profileId,
            string? email,
            string? phone,
            string? companyName,
            bool hasDuplicateEmail,
            int documentCount)
        {
            var signals = new List<RiskSignalEntity>();

            // HIGH signals
            if (hasDuplicateEmail)
                signals.Add(RiskSignalEntity.Create(profileId, "high",
                    "Duplicate email detected",
                    "Another active profile uses this email address.",
                    "DUPLICATE_EMAIL"));

            // MEDIUM signals
            if (string.IsNullOrWhiteSpace(companyName))
                signals.Add(RiskSignalEntity.Create(profileId, "medium",
                    "Company name missing",
                    "Organization/venue name was not provided.",
                    "MISSING_COMPANY_NAME"));

            if (documentCount == 0)
                signals.Add(RiskSignalEntity.Create(profileId, "medium",
                    "No verification documents uploaded",
                    "The applicant has not uploaded any verification documents.",
                    "NO_DOCUMENTS"));

            // LOW signals
            if (string.IsNullOrWhiteSpace(phone))
                signals.Add(RiskSignalEntity.Create(profileId, "low",
                    "Phone number not provided",
                    "No contact phone number on record.",
                    "MISSING_PHONE"));

            return signals;
        }

        public static string ComputeRiskLevel(IEnumerable<RiskSignalEntity> signals)
        {
            var list = signals.ToList();
            if (list.Any(s => s.Severity == "high")) return "H";
            if (list.Any(s => s.Severity == "medium")) return "M";
            return "L";
        }
    }
}
