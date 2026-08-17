using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Aizen.Modules.Payment.Repository.Migrations
{
    public partial class AddIbanLast4 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "IbanLast4",
                schema: "payment",
                table: "provider_payment_profiles",
                type: "character varying(4)",
                maxLength: 4,
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IbanLast4",
                schema: "payment",
                table: "provider_payment_profiles");
        }
    }
}
