using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Aizen.Modules.Payment.Repository.Migrations
{
    /// <inheritdoc />
    public partial class AddN1N4NotificationMarkers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PriceChangeReminderVersionKey",
                schema: "payment",
                table: "provider_plan_subscriptions",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "LowBudgetNotified",
                schema: "payment",
                table: "customer_benefit_budgets",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PriceChangeReminderVersionKey",
                schema: "payment",
                table: "provider_plan_subscriptions");

            migrationBuilder.DropColumn(
                name: "LowBudgetNotified",
                schema: "payment",
                table: "customer_benefit_budgets");
        }
    }
}
