using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Aizen.Modules.ServiceRequest.Repository.Migrations
{
    /// <inheritdoc />
    public partial class AddPricingAttributes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "pricing_attribute_definitions",
                schema: "servicerequest",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    NameTr = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    NameEn = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    DataType = table.Column<int>(type: "integer", nullable: false),
                    LookupGroupCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    IsRequired = table.Column<bool>(type: "boolean", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    MinValue = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    MaxValue = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    PublicId = table.Column<Guid>(type: "uuid", nullable: true),
                    ModifyHost = table.Column<string>(type: "text", nullable: true),
                    ModifyUserId = table.Column<long>(type: "bigint", nullable: true),
                    ModifyDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreateUserId = table.Column<long>(type: "bigint", nullable: true),
                    CreateHost = table.Column<string>(type: "text", nullable: true),
                    CreateDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<long>(type: "bigint", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pricing_attribute_definitions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "pricing_attribute_values",
                schema: "servicerequest",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    OfferItemId = table.Column<long>(type: "bigint", nullable: false),
                    DefinitionCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ValueLookupItemCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ValueNumber = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    ValueText = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ValueBool = table.Column<bool>(type: "boolean", nullable: true),
                    PublicId = table.Column<Guid>(type: "uuid", nullable: true),
                    ModifyHost = table.Column<string>(type: "text", nullable: true),
                    ModifyUserId = table.Column<long>(type: "bigint", nullable: true),
                    ModifyDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreateUserId = table.Column<long>(type: "bigint", nullable: true),
                    CreateHost = table.Column<string>(type: "text", nullable: true),
                    CreateDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<long>(type: "bigint", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pricing_attribute_values", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "pricing_attribute_definition_categories",
                schema: "servicerequest",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PricingAttributeDefinitionId = table.Column<long>(type: "bigint", nullable: false),
                    ServiceCategoryCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    PublicId = table.Column<Guid>(type: "uuid", nullable: true),
                    ModifyHost = table.Column<string>(type: "text", nullable: true),
                    ModifyUserId = table.Column<long>(type: "bigint", nullable: true),
                    ModifyDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreateUserId = table.Column<long>(type: "bigint", nullable: true),
                    CreateHost = table.Column<string>(type: "text", nullable: true),
                    CreateDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<long>(type: "bigint", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pricing_attribute_definition_categories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_pricing_attribute_definition_categories_pricing_attribute_d~",
                        column: x => x.PricingAttributeDefinitionId,
                        principalSchema: "servicerequest",
                        principalTable: "pricing_attribute_definitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_pricing_attribute_definition_categories_PricingAttributeDef~",
                schema: "servicerequest",
                table: "pricing_attribute_definition_categories",
                columns: new[] { "PricingAttributeDefinitionId", "ServiceCategoryCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_pricing_attribute_definition_categories_ServiceCategoryCode",
                schema: "servicerequest",
                table: "pricing_attribute_definition_categories",
                column: "ServiceCategoryCode");

            migrationBuilder.CreateIndex(
                name: "IX_pricing_attribute_definitions_Code",
                schema: "servicerequest",
                table: "pricing_attribute_definitions",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_pricing_attribute_values_OfferItemId",
                schema: "servicerequest",
                table: "pricing_attribute_values",
                column: "OfferItemId");

            migrationBuilder.CreateIndex(
                name: "IX_pricing_attribute_values_OfferItemId_DefinitionCode",
                schema: "servicerequest",
                table: "pricing_attribute_values",
                columns: new[] { "OfferItemId", "DefinitionCode" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "pricing_attribute_definition_categories",
                schema: "servicerequest");

            migrationBuilder.DropTable(
                name: "pricing_attribute_values",
                schema: "servicerequest");

            migrationBuilder.DropTable(
                name: "pricing_attribute_definitions",
                schema: "servicerequest");
        }
    }
}
