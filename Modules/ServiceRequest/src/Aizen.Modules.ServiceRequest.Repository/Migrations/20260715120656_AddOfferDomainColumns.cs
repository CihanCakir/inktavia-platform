using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Aizen.Modules.ServiceRequest.Repository.Migrations
{
    /// <inheritdoc />
    public partial class AddOfferDomainColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsDiscount",
                schema: "servicerequest",
                table: "service_request_offer_items");

            migrationBuilder.AddColumn<decimal>(
                name: "DeliveryTotal",
                schema: "servicerequest",
                table: "service_request_offers",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "DepositType",
                schema: "servicerequest",
                table: "service_request_offers",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "DepositValue",
                schema: "servicerequest",
                table: "service_request_offers",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "DiscountTotal",
                schema: "servicerequest",
                table: "service_request_offers",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "EmergencyFeeTotal",
                schema: "servicerequest",
                table: "service_request_offers",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "GrandTotal",
                schema: "servicerequest",
                table: "service_request_offers",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "InspectionTotal",
                schema: "servicerequest",
                table: "service_request_offers",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "InstallationTotal",
                schema: "servicerequest",
                table: "service_request_offers",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "LaborTotal",
                schema: "servicerequest",
                table: "service_request_offers",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "OtherTotal",
                schema: "servicerequest",
                table: "service_request_offers",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "PaymentTermsNote",
                schema: "servicerequest",
                table: "service_request_offers",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ProductTotal",
                schema: "servicerequest",
                table: "service_request_offers",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<DateTime>(
                name: "RevisionRequestedAt",
                schema: "servicerequest",
                table: "service_request_offers",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ServiceTotal",
                schema: "servicerequest",
                table: "service_request_offers",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<DateTime>(
                name: "SubmittedAt",
                schema: "servicerequest",
                table: "service_request_offers",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Subtotal",
                schema: "servicerequest",
                table: "service_request_offers",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "TaxTotal",
                schema: "servicerequest",
                table: "service_request_offers",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<DateTime>(
                name: "ViewedAt",
                schema: "servicerequest",
                table: "service_request_offers",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WarrantyNote",
                schema: "servicerequest",
                table: "service_request_offers",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                schema: "servicerequest",
                table: "service_request_offers",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AlterColumn<decimal>(
                name: "Quantity",
                schema: "servicerequest",
                table: "service_request_offer_items",
                type: "numeric(12,3)",
                precision: 12,
                scale: 3,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<decimal>(
                name: "DiscountAmount",
                schema: "servicerequest",
                table: "service_request_offer_items",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "DiscountType",
                schema: "servicerequest",
                table: "service_request_offer_items",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "DiscountValue",
                schema: "servicerequest",
                table: "service_request_offer_items",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "LineSubtotal",
                schema: "servicerequest",
                table: "service_request_offer_items",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "LineTotal",
                schema: "servicerequest",
                table: "service_request_offer_items",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "TaxAmount",
                schema: "servicerequest",
                table: "service_request_offer_items",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "TaxRate",
                schema: "servicerequest",
                table: "service_request_offer_items",
                type: "numeric(9,4)",
                precision: 9,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "UnitCode",
                schema: "servicerequest",
                table: "service_request_offer_items",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeliveryTotal",
                schema: "servicerequest",
                table: "service_request_offers");

            migrationBuilder.DropColumn(
                name: "DepositType",
                schema: "servicerequest",
                table: "service_request_offers");

            migrationBuilder.DropColumn(
                name: "DepositValue",
                schema: "servicerequest",
                table: "service_request_offers");

            migrationBuilder.DropColumn(
                name: "DiscountTotal",
                schema: "servicerequest",
                table: "service_request_offers");

            migrationBuilder.DropColumn(
                name: "EmergencyFeeTotal",
                schema: "servicerequest",
                table: "service_request_offers");

            migrationBuilder.DropColumn(
                name: "GrandTotal",
                schema: "servicerequest",
                table: "service_request_offers");

            migrationBuilder.DropColumn(
                name: "InspectionTotal",
                schema: "servicerequest",
                table: "service_request_offers");

            migrationBuilder.DropColumn(
                name: "InstallationTotal",
                schema: "servicerequest",
                table: "service_request_offers");

            migrationBuilder.DropColumn(
                name: "LaborTotal",
                schema: "servicerequest",
                table: "service_request_offers");

            migrationBuilder.DropColumn(
                name: "OtherTotal",
                schema: "servicerequest",
                table: "service_request_offers");

            migrationBuilder.DropColumn(
                name: "PaymentTermsNote",
                schema: "servicerequest",
                table: "service_request_offers");

            migrationBuilder.DropColumn(
                name: "ProductTotal",
                schema: "servicerequest",
                table: "service_request_offers");

            migrationBuilder.DropColumn(
                name: "RevisionRequestedAt",
                schema: "servicerequest",
                table: "service_request_offers");

            migrationBuilder.DropColumn(
                name: "ServiceTotal",
                schema: "servicerequest",
                table: "service_request_offers");

            migrationBuilder.DropColumn(
                name: "SubmittedAt",
                schema: "servicerequest",
                table: "service_request_offers");

            migrationBuilder.DropColumn(
                name: "Subtotal",
                schema: "servicerequest",
                table: "service_request_offers");

            migrationBuilder.DropColumn(
                name: "TaxTotal",
                schema: "servicerequest",
                table: "service_request_offers");

            migrationBuilder.DropColumn(
                name: "ViewedAt",
                schema: "servicerequest",
                table: "service_request_offers");

            migrationBuilder.DropColumn(
                name: "WarrantyNote",
                schema: "servicerequest",
                table: "service_request_offers");

            migrationBuilder.DropColumn(
                name: "xmin",
                schema: "servicerequest",
                table: "service_request_offers");

            migrationBuilder.DropColumn(
                name: "DiscountAmount",
                schema: "servicerequest",
                table: "service_request_offer_items");

            migrationBuilder.DropColumn(
                name: "DiscountType",
                schema: "servicerequest",
                table: "service_request_offer_items");

            migrationBuilder.DropColumn(
                name: "DiscountValue",
                schema: "servicerequest",
                table: "service_request_offer_items");

            migrationBuilder.DropColumn(
                name: "LineSubtotal",
                schema: "servicerequest",
                table: "service_request_offer_items");

            migrationBuilder.DropColumn(
                name: "LineTotal",
                schema: "servicerequest",
                table: "service_request_offer_items");

            migrationBuilder.DropColumn(
                name: "TaxAmount",
                schema: "servicerequest",
                table: "service_request_offer_items");

            migrationBuilder.DropColumn(
                name: "TaxRate",
                schema: "servicerequest",
                table: "service_request_offer_items");

            migrationBuilder.DropColumn(
                name: "UnitCode",
                schema: "servicerequest",
                table: "service_request_offer_items");

            migrationBuilder.AlterColumn<int>(
                name: "Quantity",
                schema: "servicerequest",
                table: "service_request_offer_items",
                type: "integer",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(12,3)",
                oldPrecision: 12,
                oldScale: 3);

            migrationBuilder.AddColumn<bool>(
                name: "IsDiscount",
                schema: "servicerequest",
                table: "service_request_offer_items",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }
    }
}
