using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Aizen.Modules.ServiceRequest.Repository.Migrations
{
    /// <inheritdoc />
    public partial class AddOfferLineEconomics : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "CommissionBaseTotal",
                schema: "servicerequest",
                table: "service_request_offers",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "CommissionBaseAmount",
                schema: "servicerequest",
                table: "service_request_offer_items",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "CommissionEligibility",
                schema: "servicerequest",
                table: "service_request_offer_items",
                type: "integer",
                nullable: false,
                defaultValue: 3);

            migrationBuilder.AddColumn<int>(
                name: "PricingMethod",
                schema: "servicerequest",
                table: "service_request_offer_items",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            // ── BE-S1 backfill (append-only; PricingMethod already defaulted to Fixed=1) ──
            // 1) Commission eligibility by the item-type default map (Eligible=1 / Exempt=2 / InheritFromCategory=3).
            migrationBuilder.Sql(@"
UPDATE servicerequest.service_request_offer_items SET ""CommissionEligibility"" = CASE ""ItemType""
    WHEN 1 THEN 1   -- Service        -> Eligible
    WHEN 5 THEN 1   -- Labor          -> Eligible
    WHEN 3 THEN 1   -- Installation   -> Eligible
    WHEN 6 THEN 1   -- Inspection     -> Eligible
    WHEN 7 THEN 1   -- EmergencyFee   -> Eligible
    WHEN 4 THEN 1   -- Delivery       -> Eligible
    WHEN 2 THEN 3   -- Product        -> InheritFromCategory
    WHEN 9 THEN 3   -- Consumable     -> InheritFromCategory
    WHEN 99 THEN 3  -- Other          -> InheritFromCategory
    ELSE 2          -- Travel/External/EquipmentRental/MarinaOrLiftFee/OtherApprovedExpense/Discount -> Exempt
END;");

            // 2) Commission base = pre-tax post-line-discount revenue share for eligible lines, 0 for exempt.
            migrationBuilder.Sql(@"
UPDATE servicerequest.service_request_offer_items
SET ""CommissionBaseAmount"" = CASE
    WHEN ""CommissionEligibility"" = 2 THEN 0
    ELSE GREATEST(""LineSubtotal"" - ""DiscountAmount"", 0)
END;");

            // 3) Offer aggregate = sum of line CommissionBaseAmount.
            migrationBuilder.Sql(@"
UPDATE servicerequest.service_request_offers o
SET ""CommissionBaseTotal"" = COALESCE(
    (SELECT SUM(i.""CommissionBaseAmount"")
     FROM servicerequest.service_request_offer_items i
     WHERE i.""ServiceRequestOfferId"" = o.""Id""), 0);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CommissionBaseTotal",
                schema: "servicerequest",
                table: "service_request_offers");

            migrationBuilder.DropColumn(
                name: "CommissionBaseAmount",
                schema: "servicerequest",
                table: "service_request_offer_items");

            migrationBuilder.DropColumn(
                name: "CommissionEligibility",
                schema: "servicerequest",
                table: "service_request_offer_items");

            migrationBuilder.DropColumn(
                name: "PricingMethod",
                schema: "servicerequest",
                table: "service_request_offer_items");
        }
    }
}
