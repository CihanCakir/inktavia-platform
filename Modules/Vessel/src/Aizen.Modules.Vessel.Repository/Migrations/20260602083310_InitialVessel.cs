using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Aizen.Modules.Vessel.Repository.Migrations
{
    /// <inheritdoc />
    public partial class InitialVessel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "vessel");

            migrationBuilder.CreateTable(
                name: "vessels",
                schema: "vessel",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    VesselCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Slug = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    VesselTypeCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    VesselUsageTypeCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    FlagCountryCode = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    RegistrationNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    MmsiNumber = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    ImoNumber = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    CallSign = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    HomeCountryCode = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    HomeCityCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    HomeDistrictCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    HomeMarinaName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Visibility = table.Column<int>(type: "integer", nullable: false),
                    IsArchived = table.Column<bool>(type: "boolean", nullable: false),
                    ArchivedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ArchiveReason = table.Column<int>(type: "integer", nullable: true),
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
                    table.PrimaryKey("PK_vessels", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "vessel_documents",
                schema: "vessel",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    VesselId = table.Column<long>(type: "bigint", nullable: false),
                    DocumentTypeCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    DocumentName = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    FileId = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    FileName = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    FileUrl = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    MimeType = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DocumentStatus = table.Column<int>(type: "integer", nullable: false),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
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
                    table.PrimaryKey("PK_vessel_documents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_vessel_documents_vessels_VesselId",
                        column: x => x.VesselId,
                        principalSchema: "vessel",
                        principalTable: "vessels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "vessel_engines",
                schema: "vessel",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    VesselId = table.Column<long>(type: "bigint", nullable: false),
                    EngineName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    EngineTypeCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    FuelTypeCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Brand = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Model = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    SerialNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    HorsePower = table.Column<int>(type: "integer", nullable: true),
                    ProductionYear = table.Column<int>(type: "integer", nullable: true),
                    IsPrimary = table.Column<bool>(type: "boolean", nullable: false),
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
                    table.PrimaryKey("PK_vessel_engines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_vessel_engines_vessels_VesselId",
                        column: x => x.VesselId,
                        principalSchema: "vessel",
                        principalTable: "vessels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "vessel_location_snapshots",
                schema: "vessel",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    VesselId = table.Column<long>(type: "bigint", nullable: false),
                    CountryCode = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    CityCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    DistrictCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    MarinaName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Latitude = table.Column<decimal>(type: "numeric(10,7)", precision: 10, scale: 7, nullable: true),
                    Longitude = table.Column<decimal>(type: "numeric(10,7)", precision: 10, scale: 7, nullable: true),
                    AccuracyMeters = table.Column<decimal>(type: "numeric(10,3)", precision: 10, scale: 3, nullable: true),
                    Source = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CapturedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsCurrent = table.Column<bool>(type: "boolean", nullable: false),
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
                    table.PrimaryKey("PK_vessel_location_snapshots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_vessel_location_snapshots_vessels_VesselId",
                        column: x => x.VesselId,
                        principalSchema: "vessel",
                        principalTable: "vessels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "vessel_media",
                schema: "vessel",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    VesselId = table.Column<long>(type: "bigint", nullable: false),
                    MediaType = table.Column<int>(type: "integer", nullable: false),
                    FileId = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    FileName = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    FileUrl = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    IsCover = table.Column<bool>(type: "boolean", nullable: false),
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
                    table.PrimaryKey("PK_vessel_media", x => x.Id);
                    table.ForeignKey(
                        name: "FK_vessel_media_vessels_VesselId",
                        column: x => x.VesselId,
                        principalSchema: "vessel",
                        principalTable: "vessels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "vessel_owners",
                schema: "vessel",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    VesselId = table.Column<long>(type: "bigint", nullable: false),
                    UserId = table.Column<long>(type: "bigint", nullable: false),
                    UserProfileId = table.Column<long>(type: "bigint", nullable: true),
                    Role = table.Column<int>(type: "integer", nullable: false),
                    OwnershipStatus = table.Column<int>(type: "integer", nullable: false),
                    IsPrimary = table.Column<bool>(type: "boolean", nullable: false),
                    InvitedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    AcceptedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RemovedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
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
                    table.PrimaryKey("PK_vessel_owners", x => x.Id);
                    table.ForeignKey(
                        name: "FK_vessel_owners_vessels_VesselId",
                        column: x => x.VesselId,
                        principalSchema: "vessel",
                        principalTable: "vessels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "vessel_specifications",
                schema: "vessel",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    VesselId = table.Column<long>(type: "bigint", nullable: false),
                    Brand = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Model = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ProductionYear = table.Column<int>(type: "integer", nullable: true),
                    LengthValue = table.Column<decimal>(type: "numeric(10,3)", precision: 10, scale: 3, nullable: true),
                    LengthUnitCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    BeamValue = table.Column<decimal>(type: "numeric(10,3)", precision: 10, scale: 3, nullable: true),
                    BeamUnitCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    DraftValue = table.Column<decimal>(type: "numeric(10,3)", precision: 10, scale: 3, nullable: true),
                    DraftUnitCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    WeightValue = table.Column<decimal>(type: "numeric(14,3)", precision: 14, scale: 3, nullable: true),
                    WeightUnitCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    CabinCount = table.Column<int>(type: "integer", nullable: true),
                    BedCount = table.Column<int>(type: "integer", nullable: true),
                    BathroomCount = table.Column<int>(type: "integer", nullable: true),
                    HullMaterialCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    FuelCapacityValue = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: true),
                    FuelCapacityUnitCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    WaterCapacityValue = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: true),
                    WaterCapacityUnitCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
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
                    table.PrimaryKey("PK_vessel_specifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_vessel_specifications_vessels_VesselId",
                        column: x => x.VesselId,
                        principalSchema: "vessel",
                        principalTable: "vessels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "vessel_status_histories",
                schema: "vessel",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    VesselId = table.Column<long>(type: "bigint", nullable: false),
                    FromStatus = table.Column<int>(type: "integer", nullable: true),
                    ToStatus = table.Column<int>(type: "integer", nullable: false),
                    Reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ChangedByUserId = table.Column<long>(type: "bigint", nullable: true),
                    ChangedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
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
                    table.PrimaryKey("PK_vessel_status_histories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_vessel_status_histories_vessels_VesselId",
                        column: x => x.VesselId,
                        principalSchema: "vessel",
                        principalTable: "vessels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_vessel_documents_ExpiresAt",
                schema: "vessel",
                table: "vessel_documents",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_vessel_documents_VesselId_DocumentTypeCode",
                schema: "vessel",
                table: "vessel_documents",
                columns: new[] { "VesselId", "DocumentTypeCode" });

            migrationBuilder.CreateIndex(
                name: "IX_vessel_engines_VesselId_IsPrimary",
                schema: "vessel",
                table: "vessel_engines",
                columns: new[] { "VesselId", "IsPrimary" });

            migrationBuilder.CreateIndex(
                name: "IX_vessel_engines_VesselId_SerialNumber",
                schema: "vessel",
                table: "vessel_engines",
                columns: new[] { "VesselId", "SerialNumber" });

            migrationBuilder.CreateIndex(
                name: "IX_vessel_location_snapshots_VesselId_CapturedAt",
                schema: "vessel",
                table: "vessel_location_snapshots",
                columns: new[] { "VesselId", "CapturedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_vessel_location_snapshots_VesselId_IsCurrent",
                schema: "vessel",
                table: "vessel_location_snapshots",
                columns: new[] { "VesselId", "IsCurrent" });

            migrationBuilder.CreateIndex(
                name: "IX_vessel_media_VesselId_IsCover",
                schema: "vessel",
                table: "vessel_media",
                columns: new[] { "VesselId", "IsCover" });

            migrationBuilder.CreateIndex(
                name: "IX_vessel_media_VesselId_MediaType",
                schema: "vessel",
                table: "vessel_media",
                columns: new[] { "VesselId", "MediaType" });

            migrationBuilder.CreateIndex(
                name: "IX_vessel_owners_UserId",
                schema: "vessel",
                table: "vessel_owners",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_vessel_owners_VesselId_IsPrimary",
                schema: "vessel",
                table: "vessel_owners",
                columns: new[] { "VesselId", "IsPrimary" });

            migrationBuilder.CreateIndex(
                name: "IX_vessel_owners_VesselId_UserId",
                schema: "vessel",
                table: "vessel_owners",
                columns: new[] { "VesselId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_vessel_specifications_VesselId",
                schema: "vessel",
                table: "vessel_specifications",
                column: "VesselId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_vessel_status_histories_VesselId_ChangedAt",
                schema: "vessel",
                table: "vessel_status_histories",
                columns: new[] { "VesselId", "ChangedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_vessels_ImoNumber",
                schema: "vessel",
                table: "vessels",
                column: "ImoNumber");

            migrationBuilder.CreateIndex(
                name: "IX_vessels_MmsiNumber",
                schema: "vessel",
                table: "vessels",
                column: "MmsiNumber");

            migrationBuilder.CreateIndex(
                name: "IX_vessels_RegistrationNumber",
                schema: "vessel",
                table: "vessels",
                column: "RegistrationNumber");

            migrationBuilder.CreateIndex(
                name: "IX_vessels_Slug",
                schema: "vessel",
                table: "vessels",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_vessels_Status",
                schema: "vessel",
                table: "vessels",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_vessels_VesselCode",
                schema: "vessel",
                table: "vessels",
                column: "VesselCode",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "vessel_documents",
                schema: "vessel");

            migrationBuilder.DropTable(
                name: "vessel_engines",
                schema: "vessel");

            migrationBuilder.DropTable(
                name: "vessel_location_snapshots",
                schema: "vessel");

            migrationBuilder.DropTable(
                name: "vessel_media",
                schema: "vessel");

            migrationBuilder.DropTable(
                name: "vessel_owners",
                schema: "vessel");

            migrationBuilder.DropTable(
                name: "vessel_specifications",
                schema: "vessel");

            migrationBuilder.DropTable(
                name: "vessel_status_histories",
                schema: "vessel");

            migrationBuilder.DropTable(
                name: "vessels",
                schema: "vessel");
        }
    }
}
