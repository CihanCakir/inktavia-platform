# Payment Module — Endpoint Smoke Test Reference

Mock seed produces: **16 transactions**, **6 payouts**, **9 subscriptions**, **3 refund records**.  
Base URL: `https://localhost:5001` (adjust to your local port).  
All endpoints require `Authorization: Bearer <admin-jwt>`.

---

## 0. Auth — Get Admin JWT

```http
POST /api/v1/identity/auth/login
Content-Type: application/json

{
  "email": "admin@inktavia.com",
  "password": "Admin@123!"
}
```

Use the returned `accessToken` as the `Bearer` token in all calls below.

---

## 1. Dashboard KPIs

```http
GET /api/v1/payment/admin/dashboard/kpis
```

**Expected figures from mock data:**

| Metric | Value |
|---|---|
| TotalGrossVolume | ≥ ₺294,500 (all SR escrow txs) |
| TotalCommission | varies by commRate |
| CapturedEscrowCount | 4 (TX1, TX5, TX9 + TX16) |
| ReleasedCount | 5 (TX2, TX3, TX4, TX7, TX11) |
| PendingIntentCount | 1 (TX6 — SR9007) |
| FailedCount | 1 (TX8 — SR9009) |

---

## 2. Transaction Stats

```http
GET /api/v1/payment/admin/transactions/stats
```

Returns NetLiquidity, PendingClearances, OperationalBurn, FleetRoi KPI cards.

---

## 3. Subscription Stats

```http
GET /api/v1/payment/admin/subscriptions/stats
```

**Expected:**
- ActiveProviderSubscriptions: 2 (11011→STANDARD, 11012→PREMIUM)
- ActiveParticipantSubscriptions: 2 (11003→GOLD, 11004→PLATINUM)
- PastDueCount: 1 (11005)
- CancelledCount: 1 (11006)
- ExpiredProviderCount: 2, ExpiredParticipantCount: 1

---

## 4. Payout List

```http
GET /api/v1/payment/payouts
```

**Expected: 6 records**

| PayoutCode | Provider | Amount | Status |
|---|---|---|---|
| PAYOUT-001 | 11012 | ₺57,352 | Completed |
| PAYOUT-002 | 11011 | ₺12,840 | Completed |
| PAYOUT-003 | 11011 | ₺17,248 | Processing |
| PAYOUT-004 | 11011 | ₺7,276 | OnHold |
| PAYOUT-005 | 11013 | ₺8,930 | Pending |
| PAYOUT-006 | 11011 | ₺38,520 | Failed |

**Filter by status:**

```http
GET /api/v1/payment/payouts?status=OnHold
GET /api/v1/payment/payouts?status=Pending
GET /api/v1/payment/payouts?providerId=11011
```

**Legacy pending endpoint:**
```http
GET /api/v1/payment/payouts/pending
```
Returns PAYOUT-005 only (status=Pending).

---

## 5. Payout Stats

```http
GET /api/v1/payment/payouts/stats
```

Expected: TotalPending=1, TotalOnHold=1, TotalProcessing=1, TotalCompleted=2, TotalFailed=1.

---

## 6. Payout Detail

First query the list to get actual DB IDs, then:

```http
GET /api/v1/payment/payouts/{id}
```

For PAYOUT-004 (OnHold) detail, verify `HoldReason` = "AML compliance review — transaction value threshold exceeded."

---

## 7. Hold a Pending Payout (PAYOUT-005)

Get PAYOUT-005 ID from list (status=Pending, provider=11013).

```http
POST /api/v1/payment/payouts/{payout-005-id}/hold
Content-Type: application/json

{
  "reason": "Manual compliance check before CargoDry partner payout",
  "adminNote": "Holding pending partner verification completion"
}
```

**Expected:** 200 OK, payout status changes to `OnHold`.

---

## 8. Approve Manual Payout (PAYOUT-004 — OnHold)

Get PAYOUT-004 ID from list (status=OnHold).

```http
POST /api/v1/payment/payouts/{payout-004-id}/approve-manual
Content-Type: application/json

{
  "gatewayPayoutId": "MANUAL-WIRE-TEST-001",
  "adminNote": "AML check passed — manual bank transfer authorized"
}
```

**Expected:** 200 OK, payout status changes to `Completed`.

---

## 9. Mark Payout Complete (PAYOUT-003 — Processing)

Get PAYOUT-003 ID from list (status=Processing).

```http
POST /api/v1/payment/payouts/{payout-003-id}/complete
Content-Type: application/json

{
  "gatewayPayoutId": "IYZICO-PAYOUT-BATCH-0003",
  "adminNote": "Batch job confirmed transfer"
}
```

**Expected:** 200 OK, status → Completed.

---

## 10. Refund History for SR 9010 Transaction

Get TX9 ID (TransactionCode = `TXN-20260601-0009`). Query the transaction list or get via ServiceRequest 9010.

```http
GET /api/v1/payment/admin/transactions/{tx9-id}/refund-history
```

**Expected: 3 refund records**

| RefundCode | Amount | Status | Reason |
|---|---|---|---|
| REF-20260601-0001 | ₺8,700 | Processed | PartialServiceDelivered |
| REF-20260601-0002 | ₺2,900 | Processed | CompensationCredit |
| REF-20260601-0003 | ₺1,450 | Reversed | AdminForced |

TotalRefundedAmount on TX9: ₺10,150 (REF-003 reversed, so only REF-001 + REF-002 count).  
TX9 Status: `PartiallyRefunded`.

---

## 11. Issue a New Refund on TX9

TX9 is still `PartiallyRefunded` with ₺18,850 remaining net scope. Test issuing an additional refund:

```http
POST /api/v1/payment/admin/transactions/{tx9-id}/refund
Content-Type: application/json

{
  "refundAmount": 5000.00,
  "reason": 2,
  "refundType": 1,
  "adminNote": "Additional partial refund — payer dispute resolved"
}
```

`reason` values: `1=CustomerRequest, 2=PartialServiceDelivered, 3=CompensationCredit, 4=AdminForced`  
`refundType` values: `1=Partial, 2=Full`

---

## 12. Reverse a Refund Record

Get REF-20260601-0002 ID from refund history above (status=Processed).

```http
POST /api/v1/payment/admin/refund-records/{ref2-id}/reverse
Content-Type: application/json

{
  "reversalReason": "Issued in error — goodwill credit policy changed",
  "adminNote": "Admin-initiated reversal before bank settlement"
}
```

**Expected:** 200 OK, REF-002 status → Reversed, TX9 TotalRefundedAmount recalculates to ₺8,700.

---

## 13. Cancel PendingIntent TX (TX6 — SR9007)

Get TX6 ID (TransactionCode = `TXN-20260601-0006`, status=PendingIntent).

```http
POST /api/v1/payment/admin/transactions/{tx6-id}/cancel
Content-Type: application/json

{
  "cancellationReason": 1,
  "adminNote": "Test cancellation — payer request simulation"
}
```

`cancellationReason`: `1=PayerRequest, 2=ProviderRequest, 3=SystemTimeout, 4=AdminForced, 5=AbandonedIntent, 6=FraudSuspicion, 7=ServiceRequestCancelled, 8=DuplicateDetected`

**Expected:** TX6 status → Cancelled.

---

## 14. Reinstate Cancelled TX

After step 13, get TX6 ID:

```http
POST /api/v1/payment/admin/transactions/{tx6-id}/reinstate
Content-Type: application/json

{
  "adminNote": "Reinstate for payer retry — cancellation was premature"
}
```

**Expected:** TX6 status → PendingIntent.

---

## Transaction Code → SR Mapping Reference

| TxCode | SR | Payer | Provider | Gross | Status |
|---|---|---|---|---|---|
| TXN-20260601-0001 | 9001 Engine | 11003 | 11011 | ₺45,000 | Captured |
| TXN-20260601-0002 | 9003 Navigation | 11004 | 11012 | ₺67,000 | Released |
| TXN-20260601-0003 | 9004 Hull | 11005 | 11011 | ₺15,000 | Released |
| TXN-20260601-0004 | 9005 Rigging | 11006 | 11011 | ₺22,000 | Released (18% comm) |
| TXN-20260601-0005 | 9006 Propulsion | 11007 | 11012 | ₺38,000 | Captured |
| TXN-20260601-0006 | 9007 Upholstery | 11008 | 11011 | ₺12,500 | PendingIntent |
| TXN-20260601-0007 | 9008 Safety | 11003 | 11011 | ₺8,500 | Released |
| TXN-20260601-0008 | 9009 Water | 11004 | 11012 | ₺18,000 | Failed |
| TXN-20260601-0009 | 9010 Windlass | 11005 | 11013 | ₺29,000 | PartiallyRefunded |
| TXN-20260601-0010 | 9002 Electrical | 11006 | 11012 | ₺28,500 | Cancelled |
| TXN-20260601-0011 | 30005 CargoDry | 11007 | 11013 | ₺9,500 | Released (5% comm) |
| TXN-20260601-0012 | Sub Provider | 11011 | — | ₺499 | Captured |
| TXN-20260601-0013 | Sub Provider | 11012 | — | ₺999 | Captured |
| TXN-20260601-0014 | Sub Participant | 11003 | — | ₺199 | Captured |
| TXN-20260601-0015 | Sub Participant | 11004 | — | ₺399 | Captured |
| TXN-20260601-0016 | CargoDry Renew | 10003 | — | ₺350 | Captured |

## Profile ID Reference

| ID | Name | Role |
|---|---|---|
| 11003 | Ayşe Demir | Participant (Payer) |
| 11004 | Mehmet Kaya | Participant (Payer) |
| 11005 | Deniz Yılmaz | Participant (Payer) |
| 11006 | Selin Uzun | Participant (Payer) |
| 11007 | Burak Arslan | Participant (Payer) |
| 11008 | Fatma Çelik | Participant (Payer) |
| 11011 | Marina Ops | Provider (STANDARD plan) |
| 11012 | Teknik Servis | Provider (PREMIUM plan) |
| 11013 | CargoDry Ekip | Provider (FREE plan) |

---

## Build Verification

```bash
dotnet build Modules/Payment/src/Aizen.Modules.Payment/Aizen.Modules.Payment.csproj --no-incremental
```

Expected: 0 errors. Seed runs automatically on app startup via `SeedPaymentAsync`.
