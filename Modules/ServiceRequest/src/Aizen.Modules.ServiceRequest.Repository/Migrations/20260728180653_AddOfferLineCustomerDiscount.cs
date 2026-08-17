using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Aizen.Modules.ServiceRequest.Repository.Migrations
{
    /// <inheritdoc />
    public partial class AddOfferLineCustomerDiscount : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "TotalCustomerDiscount",
                schema: "servicerequest",
                table: "service_request_offers",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalPlatformFundedDiscount",
                schema: "servicerequest",
                table: "service_request_offers",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalProviderFundedDiscount",
                schema: "servicerequest",
                table: "service_request_offers",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "CustomerDiscountAmount",
                schema: "servicerequest",
                table: "service_request_offer_items",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "LineDiscountEligibility",
                schema: "servicerequest",
                table: "service_request_offer_items",
                type: "integer",
                nullable: false,
                defaultValue: 3);

            migrationBuilder.AddColumn<decimal>(
                name: "PlatformFundedDiscountAmount",
                schema: "servicerequest",
                table: "service_request_offer_items",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "ProviderFundedDiscountAmount",
                schema: "servicerequest",
                table: "service_request_offer_items",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            // ── BE-S6 backfill: LineDiscountEligibility by ItemType default map (Eligible=1, Exempt=2, Inherit=3). ──
            // Eligible: Service(1) Product(2) Installation(3) Delivery(4) Labor(5) Inspection(6) EmergencyFee(7) Consumable(9)
            // Exempt: Discount(8) Travel(10) ExternalService(11) EquipmentRental(12) MarinaOrLiftFee(13) OtherApprovedExpense(14)
            // InheritFromCategory: Other(99). Amounts stay 0 (computed at preview/P8).
            migrationBuilder.Sql(@"
                UPDATE servicerequest.service_request_offer_items
                SET ""LineDiscountEligibility"" = CASE
                    WHEN ""ItemType"" IN (1,2,3,4,5,6,7,9)      THEN 1
                    WHEN ""ItemType"" IN (8,10,11,12,13,14)     THEN 2
                    ELSE 3
                END;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TotalCustomerDiscount",
                schema: "servicerequest",
                table: "service_request_offers");

            migrationBuilder.DropColumn(
                name: "TotalPlatformFundedDiscount",
                schema: "servicerequest",
                table: "service_request_offers");

            migrationBuilder.DropColumn(
                name: "TotalProviderFundedDiscount",
                schema: "servicerequest",
                table: "service_request_offers");

            migrationBuilder.DropColumn(
                name: "CustomerDiscountAmount",
                schema: "servicerequest",
                table: "service_request_offer_items");

            migrationBuilder.DropColumn(
                name: "LineDiscountEligibility",
                schema: "servicerequest",
                table: "service_request_offer_items");

            migrationBuilder.DropColumn(
                name: "PlatformFundedDiscountAmount",
                schema: "servicerequest",
                table: "service_request_offer_items");

            migrationBuilder.DropColumn(
                name: "ProviderFundedDiscountAmount",
                schema: "servicerequest",
                table: "service_request_offer_items");
        }
    }
}
