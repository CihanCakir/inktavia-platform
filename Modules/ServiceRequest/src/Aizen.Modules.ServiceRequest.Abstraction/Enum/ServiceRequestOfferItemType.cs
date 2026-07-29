
namespace Aizen.Modules.ServiceRequest.Abstraction.Enum;

[DocumentationInfo("ServiceRequest offer item type enum", "Classification of a line item within a service offer.")]
public enum ServiceRequestOfferItemType
{
    Service = 1,
    Product = 2,
    Installation = 3,
    Delivery = 4,
    Labor = 5,
    Inspection = 6,
    EmergencyFee = 7,
    Discount = 8,

    // ── Expense / pass-through line types (§20.3, BE-S1) ──────────────────────
    // Priced lines (NOT discount lines). Per-type aggregate exposure is optional — these roll into OtherTotal
    // (no new per-type columns in S1). Economic role drives the default commission eligibility (admin-tunable):
    Consumable           = 9,   // goods consumed doing the work → InheritFromCategory
    Travel               = 10,  // pass-through expense → Exempt
    ExternalService      = 11,  // subcontracted / third-party pass-through → Exempt
    EquipmentRental      = 12,  // rented equipment pass-through → Exempt
    MarinaOrLiftFee      = 13,  // marina / lift facility fee pass-through → Exempt
    OtherApprovedExpense = 14,  // misc approved reimbursable expense → Exempt

    Other = 99
}
