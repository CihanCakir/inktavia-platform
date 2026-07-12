using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Aizen.Modules.FileStorage.Repository.Migrations
{
    /// <inheritdoc />
    public partial class BackfillFilePublicId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "file_storage");

            migrationBuilder.CreateTable(
                name: "files",
                schema: "file_storage",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FileCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    OriginalFileName = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    StoredFileName = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    BucketName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ObjectKey = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    ContentType = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Extension = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    SizeInBytes = table.Column<long>(type: "bigint", nullable: false),
                    Checksum = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    StorageProvider = table.Column<int>(type: "integer", nullable: false),
                    Visibility = table.Column<int>(type: "integer", nullable: false),
                    Category = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    UploadedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UploadedByUserId = table.Column<long>(type: "bigint", nullable: true),
                    DeletedByUserId = table.Column<long>(type: "bigint", nullable: true),
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
                    table.PrimaryKey("PK_files", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "file_access_policies",
                schema: "file_storage",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FileId = table.Column<long>(type: "bigint", nullable: false),
                    Visibility = table.Column<int>(type: "integer", nullable: false),
                    AllowedOwnerModule = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    AllowedOwnerEntityType = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    AllowedOperations = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
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
                    table.PrimaryKey("PK_file_access_policies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_file_access_policies_files_FileId",
                        column: x => x.FileId,
                        principalSchema: "file_storage",
                        principalTable: "files",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "file_owner_references",
                schema: "file_storage",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FileId = table.Column<long>(type: "bigint", nullable: false),
                    OwnerModule = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    OwnerEntityType = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    OwnerEntityId = table.Column<Guid>(type: "uuid", nullable: false),
                    LinkedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LinkedByUserId = table.Column<long>(type: "bigint", nullable: true),
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
                    table.PrimaryKey("PK_file_owner_references", x => x.Id);
                    table.ForeignKey(
                        name: "FK_file_owner_references_files_FileId",
                        column: x => x.FileId,
                        principalSchema: "file_storage",
                        principalTable: "files",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "file_processing_jobs",
                schema: "file_storage",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FileId = table.Column<long>(type: "bigint", nullable: false),
                    ProcessingType = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    RequestedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ErrorCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ErrorMessage = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    ResultDocumentId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    RetryCount = table.Column<int>(type: "integer", nullable: false),
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
                    table.PrimaryKey("PK_file_processing_jobs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_file_processing_jobs_files_FileId",
                        column: x => x.FileId,
                        principalSchema: "file_storage",
                        principalTable: "files",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "file_upload_sessions",
                schema: "file_storage",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FileId = table.Column<long>(type: "bigint", nullable: false),
                    UploadSessionCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    BucketName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ObjectKey = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    RequestedFileName = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    RequestedContentType = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    RequestedSizeInBytes = table.Column<long>(type: "bigint", nullable: false),
                    RequestedByUserId = table.Column<long>(type: "bigint", nullable: true),
                    ClientId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    DeviceId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
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
                    table.PrimaryKey("PK_file_upload_sessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_file_upload_sessions_files_FileId",
                        column: x => x.FileId,
                        principalSchema: "file_storage",
                        principalTable: "files",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "file_versions",
                schema: "file_storage",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FileId = table.Column<long>(type: "bigint", nullable: false),
                    VersionNo = table.Column<int>(type: "integer", nullable: false),
                    BucketName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ObjectKey = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    SizeInBytes = table.Column<long>(type: "bigint", nullable: false),
                    Checksum = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    CreatedByUserId = table.Column<long>(type: "bigint", nullable: true),
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
                    table.PrimaryKey("PK_file_versions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_file_versions_files_FileId",
                        column: x => x.FileId,
                        principalSchema: "file_storage",
                        principalTable: "files",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_file_access_policies_FileId",
                schema: "file_storage",
                table: "file_access_policies",
                column: "FileId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_file_owner_references_FileId_OwnerModule_OwnerEntityType_Ow~",
                schema: "file_storage",
                table: "file_owner_references",
                columns: new[] { "FileId", "OwnerModule", "OwnerEntityType", "OwnerEntityId" });

            migrationBuilder.CreateIndex(
                name: "IX_file_owner_references_OwnerModule_OwnerEntityType_OwnerEnti~",
                schema: "file_storage",
                table: "file_owner_references",
                columns: new[] { "OwnerModule", "OwnerEntityType", "OwnerEntityId" });

            migrationBuilder.CreateIndex(
                name: "IX_file_processing_jobs_FileId_ProcessingType",
                schema: "file_storage",
                table: "file_processing_jobs",
                columns: new[] { "FileId", "ProcessingType" });

            migrationBuilder.CreateIndex(
                name: "IX_file_processing_jobs_Status",
                schema: "file_storage",
                table: "file_processing_jobs",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_file_upload_sessions_FileId",
                schema: "file_storage",
                table: "file_upload_sessions",
                column: "FileId");

            migrationBuilder.CreateIndex(
                name: "IX_file_upload_sessions_UploadSessionCode",
                schema: "file_storage",
                table: "file_upload_sessions",
                column: "UploadSessionCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_file_versions_FileId_VersionNo",
                schema: "file_storage",
                table: "file_versions",
                columns: new[] { "FileId", "VersionNo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_files_Category",
                schema: "file_storage",
                table: "files",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_files_FileCode",
                schema: "file_storage",
                table: "files",
                column: "FileCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_files_Status",
                schema: "file_storage",
                table: "files",
                column: "Status");

            // Backfill NULL PublicId with fresh Guids for any existing rows
            migrationBuilder.Sql(@"
                UPDATE file_storage.files
                SET ""PublicId"" = gen_random_uuid()
                WHERE ""PublicId"" IS NULL;
            ");

            // Add unique index on PublicId to prevent duplicates going forward
            migrationBuilder.CreateIndex(
                name: "IX_files_PublicId",
                schema: "file_storage",
                table: "files",
                column: "PublicId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_files_PublicId",
                schema: "file_storage",
                table: "files");

            migrationBuilder.DropTable(
                name: "file_access_policies",
                schema: "file_storage");

            migrationBuilder.DropTable(
                name: "file_owner_references",
                schema: "file_storage");

            migrationBuilder.DropTable(
                name: "file_processing_jobs",
                schema: "file_storage");

            migrationBuilder.DropTable(
                name: "file_upload_sessions",
                schema: "file_storage");

            migrationBuilder.DropTable(
                name: "file_versions",
                schema: "file_storage");

            migrationBuilder.DropTable(
                name: "files",
                schema: "file_storage");
        }
    }
}
