# Profile Performance & Priority Engine
## Research, Data Contract Audit & Roadmap

**Phase:** 18 — Research & Design (no production code)  
**Date:** 2026-07-06  
**Author:** AI Architect Pass  
**Status:** Final Draft

---

## A. Executive Summary

Inktavia Marine OS requires a fair, auditable, explainable scoring engine for all platform profiles. The MVP focus is **provider performance** — enabling admin dashboards, smarter service-request routing, and CargoDry opportunity prioritization. The engine must be designed generically from day one to support **participant/customer/owner performance** in later phases without requiring entity refactoring.

**Architecture decision:** `Profile.Performance` sub-module housed inside the existing `Profile` module scaffold. No separate module.

**Key audit findings:**

| Domain | Provider signals available today | Participant signals available today |
|--------|-----------------------------------|------------------------------------|
| ServiceRequest | Offer/assignment/completion/dispute status, timestamps | Cancellation, completion approval, dispute creation |
| CargoDry | Sales attribution, settlement discipline, inventory ratios, lifecycle events | Renewal completion, renewal cancellation |
| Payment | Payout status, failure rate, payment profile status | Invoice payment (partial) |
| Notification | Notification read status (indirect) | Notification read status (indirect) |
| Identity/Profile | Approval status, risk signals, verification documents | Same |
| Vessel | Ownership, completeness | Vessel completeness |

**MVP deliverable:** Backend scoring engine computing 14 provider metrics from existing data. Admin dashboard showing provider score snapshots. No automatic enforcement. No participant scoring in MVP.

---

## B. Architectural Decision: Profile.Performance Sub-Module

### Decision

All performance entities, score snapshots, metric definitions, and priority decision logs are placed inside the existing `Profile` module scaffold as a **sub-module**:

```
Aizen.Modules.Profile
└── Performance
    ├── Domain/Entities/Performance/
    ├── Application/Performance/
    └── (shared DbContext under Profile module)
```

### Rationale

1. **The platform scores profiles, not just providers.** Participant/customer/owner performance will follow. A `ProviderPerformance` module would require extraction and refactoring.
2. **Profile module is currently empty scaffolding.** `Profile.Performance` is the natural first inhabitant of that module — it gives the module a real purpose from day one.
3. **Score data is profile-owned.** The scoring engine does not own ServiceRequest or CargoDry transactional data. It reads signals from those modules and stores **computed profile-level metrics** only.
4. **Clean DDD boundary.** Performance snapshots and metrics reference other modules by `ProfileId` + `ProfileType` — not by provider-specific foreign keys.

### Naming Rules

| ✅ Use | ❌ Avoid |
|--------|---------|
| `ProfilePerformanceSnapshotEntity` | `ProviderPerformanceSnapshotEntity` |
| `ProfilePerformanceMetricEntity` | `ProviderMetricEntity` |
| `ProfilePerformanceScoreHistoryEntity` | `ProviderScoreHistoryEntity` |
| `ProfilePriorityDecisionLogEntity` | `ProviderPriorityDecisionLogEntity` |
| `ProfilePerformanceOverrideEntity` | `ProviderOverrideEntity` |
| `ProfileType.Provider` / `ProfileType.Participant` | provider-only field names |

---

## C. Existing Data Source Audit

### C.1 ServiceRequest Module

**Entity: `ServiceRequestEntity`**

| Field | Performance Signal | Notes |
|-------|--------------------|-------|
| `OwnerUserId` | Participant signal anchor | Links requests to participant/owner |
| `Status` | Request lifecycle | Completed, Cancelled, Expired drive provider and participant rates |
| `CancelledAt` + `CancelReason` + `CancelledByUserId` | Cancellation ownership | Distinguish owner vs provider cancellations via `CancelledByUserId` |
| `ServiceCategoryCode` | Category-specific scoring | Separate rates per category |
| `RequestedStartDate` / `RequestedEndDate` | Scheduling discipline | Compute scheduling accuracy |
| `DisputedAt` | Dispute flag | Drives dispute rate |
| `CreatedAtUtc` (from `AizenEntityWithAudit`) | Recency | For decay weighting |

**Entity: `ServiceRequestOfferEntity`**

| Field | Provider Signal | Computable? |
|-------|-----------------|-------------|
| `ProviderProfileId` | Offer anchor | ✅ |
| `Status` (Draft/Submitted/Accepted/Rejected/Withdrawn/Expired) | Offer submission rate, withdrawal rate | ✅ |
| `AcceptedAt` | Acceptance timing | ✅ |
| `ExpiresAt` | Expiry rate | ✅ |

**Entity: `ServiceRequestAssignmentEntity`**

| Field | Provider Signal | Computable? |
|-------|-----------------|-------------|
| `ProviderProfileId` | Assignment anchor | ✅ |
| `Status` (Pending/Accepted/Rejected/Scheduled/InProgress/Completed/Cancelled) | Assignment acceptance, completion discipline | ✅ |
| `ScheduledStartDate` / `ActualStartDate` | Start delay | ✅ — diff computable |
| `ScheduledEndDate` / `ActualEndDate` | On-time completion | ✅ — diff computable |
| `RejectionReason` | Rejection pattern | ✅ |
| `CancellationReason` | Cancellation discipline | ✅ |

**Entity: `ServiceRequestCompletionEntity`**

| Field | Signal | Computable? |
|-------|--------|-------------|
| `Status` (Submitted/ApprovedByOwner/RejectedByOwner/DisputedByOwner) | Completion quality | ✅ |
| `EvidenceFileId` | Completion proof | ✅ — null = no proof submitted |
| `SubmittedAt` / `ReviewedAt` | Approval delay (participant) | ✅ |
| `CompletionNotes` | Work quality indicator | Partial — requires text analysis |

**Entity: `ServiceRequestDisputeEntity`**

| Field | Signal | Computable? |
|-------|--------|-------------|
| `OpenedByActorType` (Owner/Provider/Admin) | Who opened dispute | ✅ — provider-opened disputes signal different risk |
| `Reason` | Dispute category | ✅ |
| `Status` (Open/UnderReview/Resolved) | Resolution rate | ✅ |
| `ResolvedAt` vs `OpenedAt` | Dispute resolution speed | ✅ |

**Entity: `ServiceRequestWorkLogEntity`**

| Field | Signal | Computable? |
|-------|--------|-------------|
| `LogType` | Work log completeness | ✅ — logs per assignment |
| `AttachmentFileId` | Proof attachment rate | ✅ |
| `LoggedAt` | Log frequency discipline | ✅ |

**Entity: `ServiceRequestStatusHistoryEntity`**

| Field | Signal | Computable? |
|-------|--------|-------------|
| `FromStatus` / `ToStatus` | Full transition audit | ✅ — enables response-time calculation |
| `OccurredAt` | Transition timestamp | ✅ |
| `ActorType` | Who drove transitions | ✅ |

**Entity: `ServiceRequestMessageEntity`**

| Field | Signal | Computable? |
|-------|--------|-------------|
| `SenderType` + `IsRead` + `ReadAt` | Message responsiveness | Partial — only read status, no response time |

---

### C.2 CargoDry Module

**Entity: `CargoDrySalesAttributionEntity`**

| Field | Provider Signal | Computable? |
|-------|-----------------|-------------|
| `ProviderProfileId` | Attribution anchor | ✅ |
| `SalesChannel` | Sales channel distribution | ✅ |
| `Status` (Attributed/CommercialReviewRequired/SettlementPending/Settled/Cancelled) | Commercial review rate, completion rate | ✅ |
| `CommercialReviewRequired` rate | Compliance signal | ✅ |
| `AttributedAt` | Activation timeliness | ✅ |
| `ResolvedRuleId` + `ResolvedRuleName` | Rule resolution trace | ✅ |

**Entity: `CargoDrySellThroughSettlementEntity`**

| Field | Provider Signal | Computable? |
|-------|-----------------|-------------|
| `ProviderProfileId` | Settlement anchor | ✅ |
| `TotalKitCount` / `SettledKitCount` | Sell-through rate | ✅ |
| `TotalSaleAmount` / `ProviderPayoutAmount` | Revenue contribution | ✅ |
| `Status` (Pending/ReadyForSettlement/Scheduled/Settled/Disputed/Cancelled) | Settlement discipline | ✅ |
| `DisputeReason` | Settlement dispute rate | ✅ |
| `PayoutFailureReason` | Payout issue signal | ✅ |
| `PayoutCompletedAtUtc` vs `ScheduledSettlementDate` | Settlement timeliness | ✅ |

**Entity: `CargoDryProviderInventoryEntity`**

| Field | Provider Signal | Computable? |
|-------|-----------------|-------------|
| `ProviderProfileId` | Inventory anchor | ✅ |
| `TotalAllocated` / `TotalActivated` / `TotalRevoked` / `TotalReturned` | Stock discipline | ✅ |
| `AvailableStock` (computed) | Stock turn ratio | ✅ |

**Entity: `CargoDryRenewalPreparationEntity`**

| Field | Signal | Computable? |
|-------|--------|-------------|
| `OwnerUserId` | Participant renewal anchor | ✅ |
| `ProviderProfileId` | Provider renewal contribution | ✅ |
| `Status` (Completed/Cancelled/Failed) | Renewal completion rate | ✅ |
| `NotificationStatus` | Notification response | Partial |
| `CompletedAtUtc` vs `CurrentExpiresAtUtc` | Renewal timeliness | ✅ |
| `CancellationReason` | Cancellation discipline | ✅ |

**Entity: `CargoDryKitLifecycleEventEntity`**

| Field | Signal | Computable? |
|-------|--------|-------------|
| `EventType` | Event type distribution | ✅ |
| `ActorUserId` + `ActorType` | Who drove events | ✅ |
| `OccurredAtUtc` | Lifecycle timing | ✅ |

**Entity: `CargoDryKitEntity`**

| Field | Signal | Computable? |
|-------|--------|-------------|
| `ProviderProfileId` | Provider kit portfolio | ✅ |
| `RenewalCount` | Kit retention indicator | ✅ |
| `Status` (revoked, expired) | Kit health | ✅ |

---

### C.3 Payment Module

**Entity: `PayoutRecordEntity`**

| Field | Provider Signal | Computable? |
|-------|-----------------|-------------|
| `ProviderProfileId` | Payout anchor | ✅ |
| `Status` (Pending/Approved/Processing/Completed/Failed/OnHold) | Payout health rate | ✅ |
| `FailureReason` | Failure pattern | ✅ |
| `HoldReason` | Hold pattern | ✅ |

**Entity: `InvoiceHeaderEntity`**

| Field | Participant Signal | Computable? |
|-------|-------------------|-------------|
| `BuyerUserId` | Participant invoice anchor | ✅ |
| `Status` (Issued/Paid/Overdue/Cancelled/Credited) | Payment reliability | ✅ |
| `InvoiceType` | Invoice category | ✅ |
| `DueDateUtc` vs `PaidAtUtc` (if tracked) | Payment delay | Partial — requires `PaidAtUtc` field (not yet on entity) |

**Entity: `ProviderPaymentProfileEntity`**

| Field | Provider Signal | Computable? |
|-------|-----------------|-------------|
| `Status` (Active/OnHold/Blocked) | Payment profile health | ✅ |
| `SubMerchantKey` presence | Gateway registration status | ✅ |
| `VerifiedAt` | Verification recency | ✅ |

---

### C.4 Notification Module

**Entity: `NotificationEntity`**

| Field | Signal | Computable? |
|-------|--------|-------------|
| `RecipientUserId` | Notification anchor | ✅ |
| `Status` (Sent/Failed/Read) | Delivery success rate | ✅ |
| `SentAt` / `ReadAt` | Read latency | ✅ |
| `TemplateCode` | Notification type filtering | ✅ — e.g. renewal notifications |
| `Channel` | Channel effectiveness | ✅ |

---

### C.5 Identity / Profile Module

**Entity: `UserProfileEntity`**

| Field | Signal | Computable? |
|-------|--------|-------------|
| `ApprovalStatus` (Pending/Approved/Rejected) | Profile quality gate | ✅ |
| `ProfileStatus` (Active/Inactive) | Platform status | ✅ |
| `RoleContext` | Role type (Provider/Participant/Venue) | ✅ |
| `CompanyName` | Completeness | ✅ — null = incomplete |
| `City` / `Country` | Location completeness | ✅ |
| `Bio` | Profile richness | ✅ — null = incomplete |
| `ProfilePhotoUrl` | Profile completeness | ✅ |
| `BirthDate` / `Gender` | Identity completeness | ✅ |
| `ReviewedAt` | Admin review recency | ✅ |

**Entity: `RiskSignalEntity`**

| Field | Signal | Computable? |
|-------|--------|-------------|
| `Severity` | Risk level | ✅ — active high-severity signals penalize score |
| `SignalCode` | Signal type | ✅ |
| `DetectedAt` | Signal recency | ✅ |

**Entity: `VerificationDocumentEntity`**

| Field | Signal | Computable? |
|-------|--------|-------------|
| `DocumentType` presence | Document completeness | ✅ |
| `UploadedAt` | Document recency | ✅ |

---

### C.6 Vessel Module

**Entity: `VesselEntity`**

| Field | Signal | Computable? |
|-------|--------|-------------|
| `Status` | Vessel health | ✅ — for participant profiles |
| `Documents` / `Media` collection counts | Vessel completeness | ✅ |

**Entity: `VesselOwnerEntity`**

| Field | Signal | Computable? |
|-------|--------|-------------|
| `UserId` + `IsPrimary` | Owner-vessel linkage | ✅ |
| `OwnershipStatus` | Valid ownership | ✅ |

---

## D. Provider Metric Catalog

### D.1 ServiceRequest Metrics

| MetricCode | MetricName | ProfileType | SourceEntity | RequiredFields | CurrentlyComputable | Weight | MVP |
|-----------|------------|-------------|--------------|----------------|---------------------|--------|-----|
| `SR_OFFER_SUBMISSION_RATE` | Offer Submission Rate | Provider | ServiceRequestOffer | Status=Submitted / total offers eligible | ✅ | 0.08 | ✅ |
| `SR_OFFER_ACCEPTANCE_RATE` | Offer Acceptance Rate | Provider | ServiceRequestOffer | Status=Accepted / Status=Submitted | ✅ | 0.10 | ✅ |
| `SR_ASSIGNMENT_ACCEPTANCE_RATE` | Assignment Acceptance Rate | Provider | ServiceRequestAssignment | Status=Accepted / Status≠Rejected | ✅ | 0.10 | ✅ |
| `SR_ASSIGNMENT_REJECTION_RATE` | Assignment Rejection Rate | Provider | ServiceRequestAssignment | Status=Rejected / total assigned | ✅ | -0.08 | ✅ |
| `SR_JOB_START_DELAY_HOURS` | Average Job Start Delay (hours) | Provider | ServiceRequestAssignment | ActualStartDate - ScheduledStartDate | ✅ | 0.06 | ✅ |
| `SR_COMPLETION_RATE` | Completion Rate | Provider | ServiceRequestAssignment + Completion | Status=Completed / total Accepted | ✅ | 0.12 | ✅ |
| `SR_ONTIME_COMPLETION_RATE` | On-Time Completion Rate | Provider | ServiceRequestAssignment | ActualEndDate ≤ ScheduledEndDate | ✅ | 0.08 | ✅ |
| `SR_CANCELLATION_RATE` | Cancellation Rate | Provider | ServiceRequestEntity + StatusHistory | Provider-initiated cancellations / total | ✅ | -0.10 | ✅ |
| `SR_DISPUTE_RATE` | Dispute Rate | Provider | ServiceRequestDispute | Dispute count / completed jobs | ✅ | -0.12 | ✅ |
| `SR_COMPLETION_PROOF_RATE` | Completion Proof Submission Rate | Provider | ServiceRequestCompletion | EvidenceFileId not null / total completions | ✅ | 0.05 | ✅ |
| `SR_WORK_LOG_COMPLETENESS` | Work Log Completeness | Provider | ServiceRequestWorkLog | Log count / expected per assignment | ✅ | 0.05 | ✅ |
| `SR_COMPLETION_APPROVAL_RATE` | Completion Owner Approval Rate | Provider | ServiceRequestCompletion | ApprovedByOwner / total submitted | ✅ | 0.08 | ✅ |
| `SR_DISPUTE_RESOLUTION_RATE` | Dispute Resolution Contribution | Provider | ServiceRequestDispute | Resolved disputes / opened disputes | ✅ | 0.04 | Post-MVP |
| `SR_MESSAGE_RESPONSE_RATE` | Message Responsiveness | Provider | ServiceRequestMessage | Messages responded / messages received | Partial — no response time field | 0.04 | Post-MVP |
| `SR_REPEAT_CUSTOMER_RATE` | Repeat Customer Rate | Provider | ServiceRequestEntity | Unique OwnerUserIds re-booking same provider | ✅ | 0.06 | Post-MVP |

### D.2 CargoDry Metrics

| MetricCode | MetricName | ProfileType | SourceEntity | CurrentlyComputable | Weight | MVP |
|-----------|------------|-------------|--------------|---------------------|--------|-----|
| `CD_SALES_COUNT` | Provider-Attributed Sales Count | Provider | CargoDrySalesAttribution | ✅ | 0.08 | ✅ |
| `CD_ACTIVATED_KIT_COUNT` | Activated Kits Count | Provider | CargoDryKit | ✅ | 0.06 | ✅ |
| `CD_RENEWAL_CONTRIBUTION_COUNT` | Renewal Contribution Count | Provider | CargoDryRenewalPreparation | ✅ | 0.06 | ✅ |
| `CD_RENEWAL_CONVERSION_RATE` | Renewal Conversion Rate | Provider | CargoDryRenewalPreparation | ✅ | 0.07 | ✅ |
| `CD_SELLTHROUGH_RATE` | Consignment Sell-Through Rate | Provider | CargoDryProviderInventory | ✅ — TotalActivated/TotalAllocated | 0.07 | ✅ |
| `CD_COMMERCIAL_REVIEW_RATE` | Commercial Review Required Rate | Provider | CargoDrySalesAttribution | ✅ — CommercialReviewRequired / total | -0.05 | ✅ |
| `CD_SETTLEMENT_DISCIPLINE_RATE` | Settlement Completion Discipline | Provider | CargoDrySellThroughSettlement | ✅ | 0.06 | ✅ |
| `CD_PAYOUT_ISSUE_RATE` | Payout Issue Rate | Provider | PayoutRecord | ✅ — Failed/OnHold / total payouts | -0.06 | ✅ |
| `CD_STOCK_REVOKED_RATE` | Revoked/Lost Kit Rate | Provider | CargoDryProviderInventory | ✅ — TotalRevoked/TotalAllocated | -0.05 | Post-MVP |
| `CD_SETTLEMENT_DISPUTE_RATE` | Settlement Dispute Rate | Provider | CargoDrySellThroughSettlement | ✅ — Disputed / total settlements | -0.06 | Post-MVP |

### D.3 Financial/Compliance Metrics

| MetricCode | MetricName | ProfileType | SourceEntity | CurrentlyComputable | Weight | MVP |
|-----------|------------|-------------|--------------|---------------------|--------|-----|
| `FIN_PAYOUT_FAILURE_RATE` | Payout Failure Rate | Provider | PayoutRecord | ✅ | -0.06 | ✅ |
| `FIN_PAYMENT_PROFILE_STATUS` | Payment Profile Health | Provider | ProviderPaymentProfile | ✅ — OnHold/Blocked triggers penalty | -0.08 | ✅ |
| `FIN_INVOICE_MISMATCH_RATE` | Invoice Mismatch Rate | Provider | InvoiceHeader (finance reconciliation) | Partial — mismatch flags are computed by report query | Post-MVP | 0.04 | Post-MVP |
| `FIN_VERIFICATION_COMPLETENESS` | Verification Document Completeness | Provider | VerificationDocument | ✅ | 0.04 | ✅ |
| `FIN_RISK_SIGNAL_SCORE` | Active Risk Signal Penalty | Provider | RiskSignal | ✅ — severity-weighted | -0.08 | ✅ |

---

## E. Participant Metric Catalog

### E.1 ServiceRequest Behaviour Metrics

| MetricCode | MetricName | SourceEntity | CurrentlyComputable | MVP |
|-----------|------------|--------------|---------------------|-----|
| `PAR_SR_CANCELLATION_RATE` | Request Cancellation Rate | ServiceRequestEntity | ✅ — owner-initiated cancellations | Admin visibility only |
| `PAR_SR_OFFER_RESPONSE_RATE` | Offer Response Rate | ServiceRequestOffer | ✅ — offers acted on / total received | Admin visibility only |
| `PAR_SR_COMPLETION_APPROVAL_DELAY` | Completion Approval Delay | ServiceRequestCompletion | ✅ — ReviewedAt - SubmittedAt | Admin visibility only |
| `PAR_SR_DISPUTE_CREATION_RATE` | Dispute Creation Rate | ServiceRequestDispute | ✅ — owner-opened disputes / completed | Admin visibility only |
| `PAR_SR_REPEAT_BOOKING_RATE` | Repeat Service Booking Rate | ServiceRequestEntity | ✅ — multiple SR from same userId | Admin visibility only |

### E.2 Payment / Renewal Reliability Metrics

| MetricCode | MetricName | SourceEntity | CurrentlyComputable | MVP |
|-----------|------------|--------------|---------------------|-----|
| `PAR_RENEWAL_COMPLETION_RATE` | Kit Renewal Completion Rate | CargoDryRenewalPreparation | ✅ — Completed / (Completed+Cancelled) | Admin visibility only |
| `PAR_RENEWAL_CANCELLATION_COUNT` | Cancelled Renewal Count | CargoDryRenewalPreparation | ✅ | Admin visibility only |
| `PAR_RENEWAL_DELAY_RATE` | Renewal Near-Expiry Rate | CargoDryRenewalPreparation + Kit | Partial — needs CompletedAt vs ExpiresAt | Post-MVP |
| `PAR_INVOICE_OVERDUE_COUNT` | Invoice Overdue Count | InvoiceHeader | ✅ — Status=Overdue | Admin visibility only |
| `PAR_INVOICE_PAYMENT_DELAY_DAYS` | Invoice Payment Delay | InvoiceHeader | ❌ — requires `PaidAtUtc` field on InvoiceHeader | Post-MVP |

### E.3 Profile / Vessel Completeness Metrics

| MetricCode | MetricName | SourceEntity | CurrentlyComputable | MVP |
|-----------|------------|--------------|---------------------|-----|
| `PAR_PROFILE_COMPLETENESS` | Profile Completeness Score | UserProfileEntity | ✅ — field null-check scoring | Admin visibility only |
| `PAR_VESSEL_COMPLETENESS` | Vessel Profile Completeness | VesselEntity | ✅ — Spec + Docs + Media counts | Admin visibility only |
| `PAR_DOCUMENT_COMPLETENESS` | Verification Document Completeness | VerificationDocumentEntity | ✅ | Admin visibility only |
| `PAR_RISK_SIGNAL_SCORE` | Active Risk Signal Penalty | RiskSignalEntity | ✅ | Admin visibility only |

> **Important:** Participant performance must not be used as an automatic punishment mechanism in MVP or any phase without explicit admin review. All participant metrics are admin-visibility-only for risk context.

---

## F. Generic Score Model Proposal

### Core snapshot model

```csharp
public sealed class ProfilePerformanceSnapshotEntity
{
    public long        ProfileId                  // FK to UserProfile.Id (cross-module, Id-only)
    public string      ProfileType                // "Provider" | "Participant" | "Venue"
    public decimal     OverallScore               // 0–100
    public decimal     ServiceRequestScore        // 0–100
    public decimal     CargoDryScore              // 0–100
    public decimal     CustomerSatisfactionScore  // 0–100 (ratings, when available)
    public decimal     OperationalDisciplineScore // 0–100
    public decimal     FinancialReliabilityScore  // 0–100
    public decimal     PlatformComplianceScore    // 0–100
    public decimal     RiskPenaltyScore           // 0–100 (subtracted from overall)
    public decimal     ConfidenceScore            // 0–1 (0 = cold start, 1 = high confidence)
    public string      PriorityTier               // "Platinum" | "Gold" | "Silver" | "Standard" | "Flagged"
    public int         SampleSize                 // Total signal events used in computation
    public string      PeriodType                 // "Rolling90D" | "Rolling180D" | "AllTime"
    public DateTime    PeriodStartUtc
    public DateTime    PeriodEndUtc
    public DateTime    ComputedAtUtc
    public bool        IsOverridden               // Admin override active
    public string?     MetadataJson               // Extra context (category breakdown, etc.)
}
```

### Recency Decay

All metrics apply an exponential decay weight over rolling windows:

```
W(t) = e^(-λ × age_in_days)
where λ = 0.005 (≈ 50% weight at 138 days)
```

Rolling window options:
- **Rolling 90 days** — primary window for active providers
- **Rolling 180 days** — secondary window for seasonal providers
- **All-time** — available but de-weighted by low confidence

### Minimum Sample Thresholds

| Metric group | Minimum events for non-zero score |
|-------------|-----------------------------------|
| ServiceRequest metrics | 5 completed/cancelled jobs |
| CargoDry metrics | 3 activated kit attributions |
| Financial metrics | 1 payout or invoice |
| Overall score | At least 2 metric groups with ≥ minimum |

### Cold-Start Handling

When a profile has fewer events than the minimum threshold:
1. Score = 50.0 (neutral baseline, not zero)
2. `ConfidenceScore` = 0.1 to 0.3 (low)
3. `PriorityTier` = "Standard" (never "Flagged" on cold start)
4. `MetadataJson` includes `{"coldStart": true, "missingGroups": [...]}`

---

## G. Provider Score Model

### Weighting Proposal

```
OverallScore =
  (ServiceRequestScore × 0.35)
+ (CargoDryScore × 0.25)
+ (OperationalDisciplineScore × 0.15)
+ (FinancialReliabilityScore × 0.15)
+ (PlatformComplianceScore × 0.10)
- RiskPenaltyScore
× ConfidenceMultiplier
```

### ServiceRequestScore Breakdown (0–100)

```
ServiceRequestScore =
  SR_COMPLETION_RATE              × 25
+ SR_ONTIME_COMPLETION_RATE       × 15
+ SR_OFFER_ACCEPTANCE_RATE        × 12
+ SR_ASSIGNMENT_ACCEPTANCE_RATE   × 10
+ SR_COMPLETION_APPROVAL_RATE     × 10
+ SR_WORK_LOG_COMPLETENESS        ×  8
+ SR_COMPLETION_PROOF_RATE        ×  8
+ SR_OFFER_SUBMISSION_RATE        ×  7
- SR_CANCELLATION_RATE penalty    × 15 (subtracted)
- SR_DISPUTE_RATE penalty         × 20 (subtracted)
- SR_ASSIGNMENT_REJECTION_RATE    ×  5 (subtracted)
```

### CargoDryScore Breakdown (0–100)

```
CargoDryScore =
  CD_SELLTHROUGH_RATE             × 30
+ CD_SALES_COUNT (normalized)     × 20
+ CD_RENEWAL_CONVERSION_RATE      × 20
+ CD_SETTLEMENT_DISCIPLINE_RATE   × 15
+ CD_RENEWAL_CONTRIBUTION_COUNT (norm) × 10
- CD_COMMERCIAL_REVIEW_RATE penalty × 15 (subtracted)
- CD_PAYOUT_ISSUE_RATE penalty    × 10 (subtracted)
```

### PriorityTier Thresholds (Provider)

| Tier | OverallScore | ConfidenceScore |
|------|-------------|-----------------|
| Platinum | ≥ 90 | ≥ 0.7 |
| Gold | ≥ 75 | ≥ 0.5 |
| Silver | ≥ 60 | ≥ 0.4 |
| Standard | ≥ 40 or cold start | any |
| Flagged | any + active high-severity risk signal | any |

---

## H. Participant Score Model

> **Phase constraint:** Participant scoring is admin-visibility only. No automatic enforcement, ranking, or access restriction in MVP or post-MVP without explicit product decision.

### Weighting Proposal (Post-MVP)

```
ParticipantRiskScore =
  PAR_SR_CANCELLATION_RATE        × 0.25
+ PAR_SR_DISPUTE_CREATION_RATE    × 0.25
+ PAR_RENEWAL_COMPLETION_RATE     × 0.20
+ PAR_PROFILE_COMPLETENESS        × 0.15
+ PAR_INVOICE_OVERDUE_COUNT (norm) × 0.15
- PAR_RISK_SIGNAL_SCORE
```

Interpretation:
- Low `ParticipantRiskScore` = **high risk** (many cancellations, disputes, non-renewals)
- High score = **reliable participant**

### Participant PriorityTier

| Tier | Meaning | Usage |
|------|---------|-------|
| `Reliable` | Score ≥ 80 | Admin info only |
| `Standard` | Score 50–79 | Admin info only |
| `Watch` | Score 30–49 | Admin flag for review |
| `Review` | Score < 30 or active risk signals | Manual admin review required |

---

## I. Priority Engine Proposal

### Priority Contexts

| Context | Primary consumer | Input signals |
|---------|-----------------|---------------|
| `ServiceRequestProviderRecommendation` | Admin assignment suggestions | Performance, category match, location, recency |
| `AdminAssignmentSuggestion` | Admin panel — quick suggest | Performance + availability + workload |
| `ProviderSearchRanking` | Public-facing provider search | Performance + location + category |
| `CargoDryOpportunityRouting` | CargoDry admin routing | CargoDry score, inventory, settlement discipline |
| `ParticipantRiskReview` | Admin risk queue | Participant risk score |
| `CustomerSupportPriority` | Future: support ticketing | Participant tier |

### Priority Score Formula (Provider)

```
PriorityScore =
  OverallScore                      × 0.40
+ CategoryFitScore (0–100)          × 0.20
+ LocationProximityScore (0–100)    × 0.15
+ CapacityScore (0–100)             × 0.10
+ CargoDryContributionBonus         × 0.10
- RecentWorkloadPenalty             × 0.05
+ ManualBoostValue                  (admin override, 0–20 pts)
- RiskPenalty                       (from RiskSignals)
```

### Input DTO

```csharp
public record ProfilePriorityInputDto
{
    public long        ProfileId
    public string      ProfileType
    public string      ContextType       // "ServiceRequestProviderRecommendation" | etc.
    public string?     RequiredCategoryCode
    public string?     RequiredLocationCountryCode
    public string?     RequiredLocationCityCode
    public int?        MaxResultCount
    public DateTimeOffset? RequestedAtUtc
}
```

### Output DTO

```csharp
public record ProfilePriorityResultDto
{
    public long    ProfileId
    public string  ProfileType
    public decimal PriorityScore
    public string  PriorityTier
    public decimal OverallScore
    public decimal ConfidenceScore
    public string? CategoryFitNote
    public string? LocationNote
    public bool    IsManuallyBoosted
    public string? ExplanationSummary      // Human-readable: "Top provider in Mast category, Istanbul"
    public List<string> ExplanationFactors // ["Completion rate 94%", "3 disputes in 90 days"]
}
```

### Decision Log

```csharp
public sealed class ProfilePriorityDecisionLogEntity
{
    public long        ProfileId
    public string      ProfileType
    public string      ContextType
    public decimal     FinalPriorityScore
    public string      FactorsJson         // serialized ExplanationFactors
    public string?     AdminNote
    public DateTime    DecidedAtUtc
    public long?       TriggeredByAdminUserId
    public bool        WasOverridden
}
```

---

## J. Architecture Options

### Option A — Profile module with Performance sub-module ✅ RECOMMENDED

```
Aizen.Modules.Profile
└── Performance sub-module (new entities, same module)
```

| Aspect | Assessment |
|--------|-----------|
| Pros | Generic by design, clean boundary, supports all profile types, aligns with project naming conventions, re-uses Profile DbContext |
| Cons | Requires Profile module to become active (currently stub) |
| Data ownership | Profile.Performance owns snapshots + metrics + logs. Other modules own signals. |
| Migration needs | New tables added to Profile module DbContext |
| Scalability | Snapshot table is independently queryable; score history supports trend analysis |
| Effort | Medium — 2–3 phases to MVP |
| Long-term | Best — no refactoring when participant scoring is added |
| Provider support | ✅ Full |
| Participant support | ✅ Built in via `ProfileType` enum |

---

### Option B — Separate ProviderPerformance module ❌ NOT RECOMMENDED

| Aspect | Assessment |
|--------|-----------|
| Pros | Isolated, independently deployable |
| Cons | Provider-only naming bakes in a limitation that must be refactored when participants are added; duplicates module overhead |
| Long-term | Poor — requires migration and renaming at participant phase |

---

### Option C — AdminPanel BFF aggregation only ❌ NOT RECOMMENDED

| Aspect | Assessment |
|--------|-----------|
| Pros | No new tables; computable on demand |
| Cons | No persistence, no history, no score trends, no priority queue, no override mechanism; expensive cross-service queries on every page load |
| Scalability | Poor |
| Long-term | Unsustainable beyond MVP |

---

### Option D — Extend ServiceRequest module ❌ NOT RECOMMENDED

| Aspect | Assessment |
|--------|-----------|
| Pros | Reuses SR infrastructure |
| Cons | Violates module boundary; CargoDry and Payment signals have no home; participant signals impossible |
| Long-term | Poor — incorrect domain placement |

---

## K. Recommended Architecture

**Option A: Profile.Performance sub-module.**

Module layout:

```
Modules/Profile/src/
├── Aizen.Modules.Profile.Domain/
│   └── Entities/Performance/
│       ├── ProfilePerformanceSnapshotEntity.cs
│       ├── ProfilePerformanceMetricEntity.cs
│       ├── ProfilePerformanceScoreHistoryEntity.cs
│       ├── ProfilePriorityRuleEntity.cs
│       ├── ProfilePriorityDecisionLogEntity.cs
│       └── ProfilePerformanceOverrideEntity.cs
├── Aizen.Modules.Profile.Abstraction/
│   └── Performance/
│       ├── Dto/ProfilePerformanceSnapshotDto.cs
│       ├── Dto/ProfilePerformanceMetricDto.cs
│       ├── Dto/ProfilePriorityResultDto.cs
│       └── Enum/ProfileType.cs
├── Aizen.Modules.Profile.Application/
│   └── Performance/
│       ├── Queries/GetProfilePerformanceSnapshot/
│       ├── Queries/GetProfilePerformanceMetrics/
│       ├── Queries/GetProfilePerformanceDashboard/
│       ├── Queries/GetProfilePriorityPreview/
│       ├── Commands/RecomputeProfilePerformance/
│       ├── Commands/OverrideProfilePerformance/
│       └── Services/ProfilePerformanceEngine.cs
└── Aizen.Modules.Profile.Repository/
    └── Performance/
        ├── IProfilePerformanceSnapshotRepository.cs
        └── ProfilePerformanceSnapshotRepository.cs
```

The scoring engine (`ProfilePerformanceEngine`) queries other modules via **read-only cross-module queries** (same PostgreSQL database, separate schemas). It does NOT call module HTTP APIs — it reads directly from the shared database using module-specific read models or views.

---

## L. Entity / DTO Proposal

### L.1 ProfilePerformanceSnapshotEntity

**Purpose:** Stores the latest computed performance snapshot for a profile.

```csharp
public sealed class ProfilePerformanceSnapshotEntity : AizenEntityWithAudit
{
    public long     ProfileId                  // UserProfile.Id
    public string   ProfileType                // "Provider" | "Participant"
    public string   PeriodType                 // "Rolling90D" | "Rolling180D" | "AllTime"
    public DateTime PeriodStartUtc
    public DateTime PeriodEndUtc
    public decimal  OverallScore               // 0–100
    public decimal  ServiceRequestScore
    public decimal  CargoDryScore
    public decimal  CustomerSatisfactionScore
    public decimal  OperationalDisciplineScore
    public decimal  FinancialReliabilityScore
    public decimal  PlatformComplianceScore
    public decimal  RiskPenaltyScore
    public decimal  ConfidenceScore            // 0.0–1.0
    public string   PriorityTier               // Platinum|Gold|Silver|Standard|Flagged
    public int      SampleSize
    public bool     IsOverridden
    public DateTime ComputedAtUtc
    public string?  MetadataJson
}
```

**Indexes:** `(ProfileId, ProfileType, PeriodType)` unique; `(ComputedAtUtc)` for freshness queries.  
**Retention:** Keep latest snapshot per profile. Move to `ScoreHistory` on recompute.

---

### L.2 ProfilePerformanceMetricEntity

**Purpose:** Stores individual metric values per profile per computation run.

```csharp
public sealed class ProfilePerformanceMetricEntity : AizenEntityWithAudit
{
    public long     ProfileId
    public string   ProfileType
    public long     SnapshotId              // FK to ProfilePerformanceSnapshotEntity
    public string   MetricCode              // e.g. "SR_COMPLETION_RATE"
    public string   MetricName
    public decimal  RawValue               // Unweighted metric value (e.g. 0.94 = 94%)
    public decimal  WeightedScore          // Contribution to parent score (0–100)
    public int      SampleCount            // Events used in this metric
    public string   SourceModule           // "ServiceRequest" | "CargoDry" | "Payment"
    public string?  CalculationNoteJson    // Audit of how value was derived
    public DateTime ComputedAtUtc
}
```

**Indexes:** `(ProfileId, MetricCode, ComputedAtUtc)`.

---

### L.3 ProfilePerformanceScoreHistoryEntity

**Purpose:** Immutable historical record of past snapshot values (one row per recompute).

```csharp
public sealed class ProfilePerformanceScoreHistoryEntity : AizenEntityWithAudit
{
    public long     ProfileId
    public string   ProfileType
    public decimal  OverallScore
    public string   PriorityTier
    public decimal  ConfidenceScore
    public string   PeriodType
    public DateTime PeriodStartUtc
    public DateTime PeriodEndUtc
    public DateTime ComputedAtUtc
    public string?  TriggerReason       // "ScheduledJob" | "AdminRecompute" | "EventTrigger"
}
```

---

### L.4 ProfilePriorityDecisionLogEntity

**Purpose:** Append-only log of priority scoring decisions (for explainability + audit).

```csharp
public sealed class ProfilePriorityDecisionLogEntity : AizenEntityWithAudit
{
    public long     ProfileId
    public string   ProfileType
    public string   ContextType             // "ServiceRequestProviderRecommendation" etc.
    public decimal  FinalPriorityScore
    public string   PriorityTier
    public string   FactorsJson             // JSON array of ExplanationFactors
    public string?  InputContextJson        // Serialized input (category, location, etc.)
    public bool     WasManuallyBoosted
    public decimal? ManualBoostValue
    public string?  AdminNote
    public long?    TriggeredByAdminUserId
    public DateTime DecidedAtUtc
}
```

---

### L.5 ProfilePerformanceOverrideEntity

**Purpose:** Admin-set manual override on score or tier (for appeals, exceptional circumstances).

```csharp
public sealed class ProfilePerformanceOverrideEntity : AizenEntityWithAudit
{
    public long     ProfileId
    public string   ProfileType
    public string   OverrideType           // "ScoreBoost" | "TierForce" | "MetricExclusion"
    public decimal? ScoreBoostValue
    public string?  ForcedTier
    public string?  ExcludedMetricCodesJson
    public string   Reason                 // Required
    public long     SetByAdminUserId
    public DateTime EffectiveFromUtc
    public DateTime? EffectiveUntilUtc     // Null = indefinite
    public bool     IsActive
    public string?  ReviewNote
}
```

---

### L.6 ProfilePriorityRuleEntity

**Purpose:** Admin-configurable priority weighting rules per context + profile type.

```csharp
public sealed class ProfilePriorityRuleEntity : AizenEntityWithAudit
{
    public string   RuleCode
    public string   ContextType
    public string   ProfileType
    public string   WeightsJson           // JSON: { "OverallScore": 0.40, "CategoryFit": 0.20 }
    public bool     IsActive
    public int      Priority
    public DateTime EffectiveFromUtc
    public DateTime? EffectiveUntilUtc
    public long     CreatedByAdminUserId
}
```

---

### L.7 Key DTOs

```csharp
// Snapshot response
public record ProfilePerformanceSnapshotDto
{
    public long    ProfileId
    public string  ProfileType
    public decimal OverallScore
    public string  PriorityTier
    public decimal ConfidenceScore
    public decimal ServiceRequestScore
    public decimal CargoDryScore
    public decimal FinancialReliabilityScore
    public decimal RiskPenaltyScore
    public DateTime ComputedAtUtc
    public bool    IsOverridden
    public bool    IsColdStart
    public int     SampleSize
}

// Metric detail response
public record ProfilePerformanceMetricDto
{
    public string  MetricCode
    public string  MetricName
    public decimal RawValue
    public decimal WeightedScore
    public int     SampleCount
    public string  SourceModule
    public string  Trend    // "Improving" | "Stable" | "Declining"
}

// Priority result
public record ProfilePriorityResultDto
{
    public long    ProfileId
    public string  ProfileType
    public decimal PriorityScore
    public string  PriorityTier
    public decimal OverallScore
    public decimal ConfidenceScore
    public string? ExplanationSummary
    public List<string> ExplanationFactors
    public bool    IsManuallyBoosted
}
```

---

## M. Module / BFF API Proposal

### M.1 Profile Module Endpoints

```http
# Get latest performance snapshot for a profile
GET /api/v1/profile/admin/performance/profiles/{profileId}/snapshot
    ?profileType=Provider&periodType=Rolling90D

# Get individual metric breakdown
GET /api/v1/profile/admin/performance/profiles/{profileId}/metrics
    ?profileType=Provider&metricGroup=ServiceRequest

# Score history (trend)
GET /api/v1/profile/admin/performance/profiles/{profileId}/history
    ?profileType=Provider&fromUtc=2026-01-01&toUtc=2026-07-01

# Dashboard — paginated list of provider snapshots
GET /api/v1/profile/admin/performance/dashboard
    ?profileType=Provider&tier=Gold&page=1&pageSize=50

# Priority preview for a context
GET /api/v1/profile/admin/performance/priority-preview
    ?contextType=ServiceRequestProviderRecommendation
    &categoryCode=MAST
    &locationCityCode=ISTANBUL
    &maxResults=10

# Decision logs
GET /api/v1/profile/admin/performance/decision-logs
    ?profileId=123&contextType=ServiceRequestProviderRecommendation&page=1

# Trigger recompute
POST /api/v1/profile/admin/performance/recompute
    Body: { profileId, profileType, triggerReason }

# Set admin override
POST /api/v1/profile/admin/performance/overrides
    Body: { profileId, profileType, overrideType, reason, ... }
```

### M.2 AdminPanel BFF Endpoints

```http
GET /api/v1/admin-panel/profile/performance/profiles/{profileId}/snapshot
GET /api/v1/admin-panel/profile/performance/profiles/{profileId}/metrics
GET /api/v1/admin-panel/profile/performance/profiles/{profileId}/history
GET /api/v1/admin-panel/profile/performance/dashboard
GET /api/v1/admin-panel/profile/performance/priority-preview
GET /api/v1/admin-panel/profile/performance/decision-logs
POST /api/v1/admin-panel/profile/performance/recompute
POST /api/v1/admin-panel/profile/performance/overrides
```

All endpoints:
- `[Authorize(Policy = "AdminPanelAccess")]`
- BFF controller inherits `AizenWebApiController`
- JSON endpoints return `AizenApiResponse<T>`
- BFF is proxy/orchestration only — no scoring logic

---

## N. Admin Web UX Proposal

### N.1 Profile Performance Dashboard

**Route:** `/app/performance/dashboard`  
**Purpose:** Paginated list of provider performance snapshots. Admin's primary performance monitoring surface.

**Widgets:**
- KPI strip: Platinum count, Gold count, Flagged count, Cold-Start count
- Filter bar: profileType, tier, category, search by name

**Table columns:** Profile name | Role | Overall Score | Tier badge | Confidence | Last computed | Actions

**Actions:** View detail → `/app/performance/providers/{profileId}`

---

### N.2 Provider Performance Detail

**Route:** `/app/performance/providers/{profileId}`  
**Purpose:** Full performance breakdown for a single provider.

**Sections:**
1. **Score Overview card** — Overall score gauge, PriorityTier badge, ConfidenceScore bar, IsColdStart warning
2. **Score Domain breakdown** — 6 domain scores as horizontal bars (SR, CargoDry, Financial, Operational, Compliance, Risk Penalty)
3. **Metric detail table** — MetricCode, RawValue, WeightedScore, SampleCount, Trend icon, SourceModule
4. **Score history chart** — Line chart of OverallScore over last 12 months
5. **Active overrides panel** — List of active overrides with Reason + expiry
6. **Admin actions** — Recompute button + Add Override button
7. **Decision logs** — Last 10 priority decisions affecting this profile

**States:** Loading spinner | Error banner | Cold-start info panel

---

### N.3 Participant Performance Detail

**Route:** `/app/performance/participants/{profileId}`  
**Purpose:** Risk context for a participant. Admin-only, non-punitive framing.

**Sections:**
1. **Risk Context card** — Participant risk score, tier (Reliable/Standard/Watch/Review), confidence
2. **Metric detail table** — Same structure as provider, but participant metrics
3. **Risk Signals panel** — Active signals from Identity module (Severity, SignalCode)
4. **Admin actions** — Recompute + Add flag note

---

### N.4 Priority Preview Page

**Route:** `/app/performance/priority-preview`  
**Purpose:** Admin tool to simulate ranking for a specific context.

**Inputs:** Context type selector | Category picker | Location picker | Max results  
**Output table:** Rank | Profile name | PriorityScore | Tier | Explanation summary | Boost indicator

---

### N.5 Risk Watchlist

**Route:** `/app/performance/risk-watchlist`  
**Purpose:** All profiles with `PriorityTier = "Flagged"` or `"Review"`.

**Table:** Profile name | Type | Tier | Active Risk Signals | Last recomputed | Review action

---

### N.6 Embedded Panels

**Provider Detail page** (`/app/users/providers/{profileId}`):
- Compact performance panel: Overall score, tier badge, last 3 metrics, recompute button
- Links to full performance detail

**Participant Detail page** (`/app/users/participants/{profileId}`):
- Risk context panel: Participant risk score, tier, active risk signals

---

## O. Safety / Fairness / Audit Rules

### O.1 Cold-Start Provider Treatment
- New providers (< 5 completed jobs) get `ConfidenceScore ≤ 0.3` and `OverallScore = 50.0` (neutral).
- Tier is never below `Standard` on cold start — never `Flagged` from performance alone.
- `MetadataJson` must include `{ "coldStart": true }` so UI can display appropriate messaging.

### O.2 Cold-Start Participant Treatment
- Same neutral baseline. Participant tier is `Standard` until sufficient signals.
- Participant cold-start must never show "Watch" or "Review" tier without at least 3 separate signal events.

### O.3 Minimum Sample Thresholds
- Each metric must declare a `MinSampleCount`. If `SampleCount < MinSampleCount`, the metric contributes 0 to score instead of a penalizing value.
- This prevents a single dispute from destroying a new provider's score.

### O.4 Manual Admin Override
- `ProfilePerformanceOverrideEntity` supports `ScoreBoost`, `TierForce`, and `MetricExclusion`.
- All overrides require `Reason` (required field, min 20 characters).
- Overrides must have `EffectiveUntilUtc` or an explicit `indefinite` confirmation.
- All overrides are logged with `SetByAdminUserId` and visible in the profile's override history.

### O.5 Appeal/Review Trail
- `ProfilePriorityDecisionLogEntity` is append-only. No delete permitted.
- `ProfilePerformanceScoreHistoryEntity` is append-only. Every recompute creates a new history row.
- Admin overrides include `ReviewNote` for audit trail.

### O.6 Decision Explanation
- Every priority result must include `ExplanationSummary` and `ExplanationFactors` in the response DTO.
- Factors must be human-readable, not raw metric codes: `"Completion rate 94% (last 90 days)"` not `"SR_COMPLETION_RATE=0.94"`.

### O.7 No Automatic Punishment
- Scores are informational only until explicitly wired to an enforcement action.
- Enforcement actions (e.g. hiding a provider from search) require a separate admin-configurable rule — not automatic from score threshold.
- Participant scores must never be automatically acted upon in MVP or post-MVP without explicit product decision and legal review.

### O.8 Preventing Manipulated Signals
- Disputed/cancelled service requests where the **provider was the initiating party** are weighted differently from owner-initiated cancellations.
- Admin-closed completions are excluded from provider completion rate calculations.
- A single month spike in metrics triggers `ConfidenceScore` reduction (anomaly detection flag).

### O.9 Separating Risk Visibility from Enforcement
- `PriorityTier = "Flagged"` is a visibility flag, not a block.
- Access restrictions require separate `ProfilePerformanceOverrideEntity` with `OverrideType = "TierForce"`.
- API never returns a 403 or restriction based solely on performance tier.

---

## P. MVP Roadmap

### Phase 18 — Research, Audit & Design (current)
**Goal:** Understand data sources, design the engine, produce this document.  
**Deliverables:** This document.  
**Acceptance criteria:** All metric sources audited; entity design reviewed; architecture confirmed.

---

### Phase 19 — Profile.Performance Backend Foundation
**Goal:** Stand up the Profile module with Performance sub-module — entities, repositories, scoring engine, and module API.

**Scope:**
- Activate `Aizen.Modules.Profile` (replace Class1.cs stubs)
- Create 6 entities with EF configurations and migrations
- Create `ProfilePerformanceEngine` service — compute 14 provider metrics from existing data
- Create `GetProfilePerformanceSnapshotQuery` + `GetProfilePerformanceMetricsQuery`
- Create `RecomputeProfilePerformanceCommand`
- Create `CreateProfilePerformanceOverrideCommand`
- Module controller with 4 endpoints
- Seed: 3 provider performance snapshots with varied tiers
- EF migration: `AddProfilePerformanceTables`

**Dependencies:** Profile module DbContext setup; cross-schema read access to ServiceRequest, CargoDry, Payment schemas.  
**Risks:** Cross-schema query performance; PostgreSQL schema isolation rules.  
**Acceptance criteria:** `dotnet build` passes; seed data shows correct tier distribution; recompute endpoint returns 200.

---

### Phase 20 — AdminPanel BFF + Provider Performance Admin Dashboard
**Goal:** Expose performance endpoints through BFF; build Admin Web dashboard + provider detail.

**Scope:**
- BFF remote calls + query handlers + controller endpoints
- Admin Web: `PerformanceDashboardPage`, `ProviderPerformanceDetailPage`
- Embedded panel in existing `UserFullDetailPage` (provider section)
- tsc --noEmit passes

**Dependencies:** Phase 19.  
**Acceptance criteria:** Dashboard shows real performance data; provider detail shows score breakdown.

---

### Phase 21 — Participant Performance Admin Dashboard
**Goal:** Extend scoring engine for participant metrics; add participant visibility in admin.

**Scope:**
- Add 8 participant metrics to `ProfilePerformanceEngine`
- `ParticipantPerformanceDetailPage`
- Embedded panel in `UserFullDetailPage` (participant section)
- Risk watchlist page

**Dependencies:** Phase 20.  
**Risks:** Participant scoring must be clearly non-punitive — product decision needed before wiring to any enforcement.

---

### Phase 22 — Priority Preview and Decision Logs
**Goal:** Implement priority engine with context-aware scoring; expose decision logs.

**Scope:**
- `ProfilePriorityRuleEntity` + admin CRUD
- Priority engine: ServiceRequestProviderRecommendation + AdminAssignmentSuggestion
- `PriorityPreviewPage` in Admin Web
- `DecisionLogsPage`

**Dependencies:** Phase 21.

---

## Q. Post-MVP Roadmap

### Phase 23 — ServiceRequest Provider Recommendation Integration
**Goal:** Wire provider priority score into ServiceRequest admin assignment flow.

**Scope:** Admin assignment suggestion panel in `ServiceRequestDetailPage` uses priority engine. "Recommended providers" chip based on context (category, location, score).

**Risk:** Must explicitly display confidence score so admin understands cold-start recommendations.

---

### Phase 24 — CargoDry Opportunity Routing Integration
**Goal:** Route CargoDry kit allocation and renewal opportunities toward high-performing providers.

**Scope:** When creating/reviewing consignment agreements, surface CargoDry performance score. Filter provider list by `CargoDryScore ≥ threshold`.

---

### Phase 25 — Provider Portal Performance Visibility
**Goal:** Providers can see their own performance score via the provider portal (not admin panel).

**Scope:** New BFF endpoint for provider self-view. Simplified score breakdown (no admin overrides visible). Trend chart. Appeal request flow.

**Dependencies:** Provider portal frontend (separate project).

---

### Phase 26 — Scheduled Recompute Job + Performance Events
**Goal:** Automate score freshness via a nightly job and event-triggered recomputes.

**Scope:**
- `ProfilePerformanceRecomputeJob` (nightly, configurable window)
- Event triggers: `ServiceRequestCompleted` → recompute provider + participant
- Freshness TTL config in SystemParameter
- Alert if snapshot older than configurable threshold

---

### Phase 27 — Customer-Facing Reliability Feedback (if appropriate)
**Goal:** Show participants a simplified "platform reliability" indicator (not a score).

**Scope:** Subject to product + legal review. Participant tier shown as neutral label ("Trusted Member"), not a numeric score. No public exposure of raw metrics.

---

## R. Open Questions

| # | Question | Owner | Decision needed by |
|---|----------|-------|-------------------|
| 1 | Should `PaidAtUtc` be added to `InvoiceHeaderEntity` to enable participant payment delay metric? | Backend team | Phase 19 |
| 2 | What is the minimum platform adoption before scoring is meaningful? (cold-start duration) | Product | Phase 19 |
| 3 | Should `ProfilePerformanceEngine` read directly from other schemas, or use published projections/views? | Architecture | Phase 19 |
| 4 | Does participant scoring require legal review before any enforcement use? | Legal/Product | Before Phase 21 |
| 5 | Should scores be exposed to providers before Phase 25? | Product | Phase 20 |
| 6 | Should CargoDry providers with no ServiceRequest history get a separate scoring path? | Product | Phase 19 |
| 7 | What recency decay λ is appropriate for seasonal providers (boating season)? | Data/Product | Phase 19 |
| 8 | Should `ProfilePriorityRuleEntity` weights be editable by admin, or hardcoded? | Product | Phase 22 |
| 9 | Should dispute resolution outcome (provider found at fault vs. owner found at fault) be captured in `ServiceRequestDisputeEntity`? | Backend team | Phase 19 |
| 10 | Is `ProfileType` enum defined in `Profile.Abstraction` or in a shared Core library? | Architecture | Phase 19 |

---

## S. Final Recommendation

### Which provider metrics are computable today (no new fields needed)

14 metrics are fully computable from existing entity fields:

`SR_OFFER_SUBMISSION_RATE`, `SR_OFFER_ACCEPTANCE_RATE`, `SR_ASSIGNMENT_ACCEPTANCE_RATE`, `SR_ASSIGNMENT_REJECTION_RATE`, `SR_JOB_START_DELAY_HOURS`, `SR_COMPLETION_RATE`, `SR_ONTIME_COMPLETION_RATE`, `SR_CANCELLATION_RATE`, `SR_DISPUTE_RATE`, `SR_COMPLETION_PROOF_RATE`, `SR_WORK_LOG_COMPLETENESS`, `SR_COMPLETION_APPROVAL_RATE`, `CD_SALES_COUNT`, `CD_SELLTHROUGH_RATE`, `CD_SETTLEMENT_DISCIPLINE_RATE`, `CD_PAYOUT_ISSUE_RATE`, `FIN_PAYOUT_FAILURE_RATE`, `FIN_PAYMENT_PROFILE_STATUS`, `FIN_RISK_SIGNAL_SCORE`, `FIN_VERIFICATION_COMPLETENESS`, `CD_RENEWAL_CONVERSION_RATE`

### Which participant metrics are computable today

8 metrics are computable without new fields:

`PAR_SR_CANCELLATION_RATE`, `PAR_SR_DISPUTE_CREATION_RATE`, `PAR_SR_COMPLETION_APPROVAL_DELAY`, `PAR_RENEWAL_COMPLETION_RATE`, `PAR_RENEWAL_CANCELLATION_COUNT`, `PAR_INVOICE_OVERDUE_COUNT`, `PAR_PROFILE_COMPLETENESS`, `PAR_RISK_SIGNAL_SCORE`

### Which metrics require new data/events

| Metric | Missing element |
|--------|----------------|
| `SR_MESSAGE_RESPONSE_RATE` | Response-time tracking in `ServiceRequestMessageEntity` |
| `SR_REPEAT_CUSTOMER_RATE` | Queryable, but needs efficient indexing by (OwnerUserId, ProviderProfileId) |
| `PAR_INVOICE_PAYMENT_DELAY_DAYS` | `PaidAtUtc` field missing from `InvoiceHeaderEntity` |
| `CD_SETTLEMENT_DISPUTE_RATE` | Queryable — needs `mismatchFlags` from reconciliation report (no new entity needed) |
| `PAR_RENEWAL_DELAY_RATE` | Needs derived comparison of `CompletedAtUtc` vs `CurrentExpiresAtUtc` (computable, no new field) |
| Dispute outcome attribution | `ServiceRequestDisputeEntity` needs `FaultAttribution` enum field |
| Customer satisfaction / ratings | Rating entity does not exist yet — Post-MVP |

### Recommended architecture

**Profile.Performance sub-module inside `Aizen.Modules.Profile`.** See Section K.

### Recommended MVP score model

6 domain scores → weighted OverallScore → PriorityTier. See Sections F and G.

### Recommended priority model

Context-aware priority score combining OverallScore + CategoryFit + LocationProximity + Capacity + CargoDryContribution + ManualBoost. See Section I.

### Recommended entities

6 entities as defined in Section L: `ProfilePerformanceSnapshotEntity`, `ProfilePerformanceMetricEntity`, `ProfilePerformanceScoreHistoryEntity`, `ProfilePriorityDecisionLogEntity`, `ProfilePerformanceOverrideEntity`, `ProfilePriorityRuleEntity`.

### Recommended module/BFF endpoints

8 endpoints as defined in Section M: snapshot, metrics, history, dashboard, priority-preview, decision-logs, recompute, overrides.

### Recommended Admin Web screens

6 screens + 2 embedded panels as defined in Section N.

### Risks and fairness considerations

1. **Cold-start fairness** — New providers must receive neutral scores, not penalties.
2. **Single-event distortion** — Minimum sample thresholds required before any metric contributes to score.
3. **Participant non-punishment principle** — No automated enforcement from participant scores until explicit legal + product review.
4. **Score freshness** — Staleness beyond 72 hours should be flagged in admin UI.
5. **Admin override audit** — Every override must be auditable, dated, and reversible.
6. **Explanation requirement** — Every priority decision must carry human-readable explanation factors.

### Should implementation start with Profile.Performance backend foundation or another audit pass?

**Start with Phase 19 — Profile.Performance backend foundation.** The data source audit is complete, entity design is finalized, and all 21 provider metrics are confirmed computable from existing tables. A second audit pass is not needed before implementation begins.

The only pre-implementation decision required (Open Question #1) is whether to add `PaidAtUtc` to `InvoiceHeaderEntity` in Phase 19 or defer it to Phase 21.

---

*Document: `profile-performance-priority-engine-research-roadmap.md`*  
*Scope: Inktavia Marine OS — Phase 18 Research*  
*Date: 2026-07-06*
