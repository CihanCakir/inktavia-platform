using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Aizen.Modules.Profile.Repository.Migrations
{
    /// <inheritdoc />
    public partial class AddProfilePerformanceTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(name: "profile");

            // ── profile_performance_snapshots ─────────────────────────────────
            migrationBuilder.CreateTable(
                name: "profile_performance_snapshots",
                schema: "profile",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProfileId   = table.Column<long>(type: "bigint", nullable: false),
                    ProfileType = table.Column<int>(type: "integer", nullable: false),
                    PriorityTier = table.Column<int>(type: "integer", nullable: false),
                    OverallScore               = table.Column<decimal>(type: "numeric(6,2)", nullable: false),
                    ServiceRequestScore        = table.Column<decimal>(type: "numeric(6,2)", nullable: false),
                    CargoDryScore              = table.Column<decimal>(type: "numeric(6,2)", nullable: false),
                    OperationalDisciplineScore = table.Column<decimal>(type: "numeric(6,2)", nullable: false),
                    FinancialReliabilityScore  = table.Column<decimal>(type: "numeric(6,2)", nullable: false),
                    PlatformComplianceScore    = table.Column<decimal>(type: "numeric(6,2)", nullable: false),
                    RiskPenaltyScore           = table.Column<decimal>(type: "numeric(6,2)", nullable: false),
                    ConfidenceScore = table.Column<decimal>(type: "numeric(5,4)", nullable: false),
                    ConfidenceLevel = table.Column<int>(type: "integer", nullable: false),
                    SampleSize      = table.Column<int>(type: "integer", nullable: false),
                    HasActiveRiskSignal          = table.Column<bool>(type: "boolean", nullable: false),
                    ActiveRiskSignalMaxSeverity  = table.Column<int>(type: "integer", nullable: true),
                    LastCalculatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ValidFromUtc        = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    MetadataJson        = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    PublicId    = table.Column<Guid>(type: "uuid", nullable: true),
                    ModifyHost  = table.Column<string>(type: "text", nullable: true),
                    ModifyUserId = table.Column<long>(type: "bigint", nullable: true),
                    ModifyDate  = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreateUserId = table.Column<long>(type: "bigint", nullable: true),
                    CreateHost  = table.Column<string>(type: "text", nullable: true),
                    CreateDate  = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted   = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt   = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy   = table.Column<long>(type: "bigint", nullable: true),
                    IsActive    = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_profile_performance_snapshots", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "UIX_profile_performance_snapshots_ProfileId_ProfileType",
                schema: "profile",
                table: "profile_performance_snapshots",
                columns: new[] { "ProfileId", "ProfileType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_profile_performance_snapshots_PriorityTier",
                schema: "profile",
                table: "profile_performance_snapshots",
                column: "PriorityTier");

            migrationBuilder.CreateIndex(
                name: "IX_profile_performance_snapshots_HasActiveRiskSignal",
                schema: "profile",
                table: "profile_performance_snapshots",
                column: "HasActiveRiskSignal");

            // ── profile_score_components ──────────────────────────────────────
            migrationBuilder.CreateTable(
                name: "profile_score_components",
                schema: "profile",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SnapshotId  = table.Column<long>(type: "bigint", nullable: false),
                    ProfileId   = table.Column<long>(type: "bigint", nullable: false),
                    ProfileType = table.Column<int>(type: "integer", nullable: false),
                    Category    = table.Column<int>(type: "integer", nullable: false),
                    RawScore             = table.Column<decimal>(type: "numeric(6,2)", nullable: false),
                    Weight               = table.Column<decimal>(type: "numeric(5,4)", nullable: false),
                    WeightedContribution = table.Column<decimal>(type: "numeric(8,4)", nullable: false),
                    MetricCount    = table.Column<int>(type: "integer", nullable: false),
                    MetricsJson    = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    CalculatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PublicId    = table.Column<Guid>(type: "uuid", nullable: true),
                    ModifyHost  = table.Column<string>(type: "text", nullable: true),
                    ModifyUserId = table.Column<long>(type: "bigint", nullable: true),
                    ModifyDate  = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreateUserId = table.Column<long>(type: "bigint", nullable: true),
                    CreateHost  = table.Column<string>(type: "text", nullable: true),
                    CreateDate  = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted   = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt   = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy   = table.Column<long>(type: "bigint", nullable: true),
                    IsActive    = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_profile_score_components", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_profile_score_components_SnapshotId",
                schema: "profile",
                table: "profile_score_components",
                column: "SnapshotId");

            migrationBuilder.CreateIndex(
                name: "IX_profile_score_components_ProfileId_ProfileType",
                schema: "profile",
                table: "profile_score_components",
                columns: new[] { "ProfileId", "ProfileType" });

            migrationBuilder.CreateIndex(
                name: "UIX_profile_score_components_SnapshotId_Category",
                schema: "profile",
                table: "profile_score_components",
                columns: new[] { "SnapshotId", "Category" },
                unique: true);

            // ── profile_score_history ─────────────────────────────────────────
            migrationBuilder.CreateTable(
                name: "profile_score_history",
                schema: "profile",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProfileId    = table.Column<long>(type: "bigint", nullable: false),
                    ProfileType  = table.Column<int>(type: "integer", nullable: false),
                    PriorityTier = table.Column<int>(type: "integer", nullable: false),
                    OverallScore    = table.Column<decimal>(type: "numeric(6,2)", nullable: false),
                    ConfidenceScore = table.Column<decimal>(type: "numeric(5,4)", nullable: false),
                    SampleSize      = table.Column<int>(type: "integer", nullable: false),
                    RecordedAtUtc   = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TriggerReason   = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    MetadataJson    = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    PublicId    = table.Column<Guid>(type: "uuid", nullable: true),
                    ModifyHost  = table.Column<string>(type: "text", nullable: true),
                    ModifyUserId = table.Column<long>(type: "bigint", nullable: true),
                    ModifyDate  = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreateUserId = table.Column<long>(type: "bigint", nullable: true),
                    CreateHost  = table.Column<string>(type: "text", nullable: true),
                    CreateDate  = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted   = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt   = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy   = table.Column<long>(type: "bigint", nullable: true),
                    IsActive    = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_profile_score_history", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_profile_score_history_ProfileId_ProfileType",
                schema: "profile",
                table: "profile_score_history",
                columns: new[] { "ProfileId", "ProfileType" });

            migrationBuilder.CreateIndex(
                name: "IX_profile_score_history_RecordedAtUtc",
                schema: "profile",
                table: "profile_score_history",
                column: "RecordedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_profile_score_history_PriorityTier",
                schema: "profile",
                table: "profile_score_history",
                column: "PriorityTier");

            // ── profile_decision_logs ─────────────────────────────────────────
            migrationBuilder.CreateTable(
                name: "profile_decision_logs",
                schema: "profile",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProfileId   = table.Column<long>(type: "bigint", nullable: false),
                    ProfileType = table.Column<int>(type: "integer", nullable: false),
                    EventType        = table.Column<int>(type: "integer", nullable: false),
                    EventDescription = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    PreviousTier  = table.Column<int>(type: "integer", nullable: true),
                    NewTier       = table.Column<int>(type: "integer", nullable: true),
                    PreviousScore = table.Column<decimal>(type: "numeric(6,2)", nullable: true),
                    NewScore      = table.Column<decimal>(type: "numeric(6,2)", nullable: true),
                    ActorUserId   = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    OccurredAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    MetadataJson  = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    PublicId    = table.Column<Guid>(type: "uuid", nullable: true),
                    ModifyHost  = table.Column<string>(type: "text", nullable: true),
                    ModifyUserId = table.Column<long>(type: "bigint", nullable: true),
                    ModifyDate  = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreateUserId = table.Column<long>(type: "bigint", nullable: true),
                    CreateHost  = table.Column<string>(type: "text", nullable: true),
                    CreateDate  = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted   = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt   = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy   = table.Column<long>(type: "bigint", nullable: true),
                    IsActive    = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_profile_decision_logs", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_profile_decision_logs_ProfileId_ProfileType",
                schema: "profile",
                table: "profile_decision_logs",
                columns: new[] { "ProfileId", "ProfileType" });

            migrationBuilder.CreateIndex(
                name: "IX_profile_decision_logs_EventType",
                schema: "profile",
                table: "profile_decision_logs",
                column: "EventType");

            migrationBuilder.CreateIndex(
                name: "IX_profile_decision_logs_OccurredAtUtc",
                schema: "profile",
                table: "profile_decision_logs",
                column: "OccurredAtUtc");

            // ── profile_risk_signals ──────────────────────────────────────────
            migrationBuilder.CreateTable(
                name: "profile_risk_signals",
                schema: "profile",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProfileId   = table.Column<long>(type: "bigint", nullable: false),
                    ProfileType = table.Column<int>(type: "integer", nullable: false),
                    Severity    = table.Column<int>(type: "integer", nullable: false),
                    SignalCode  = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    SourceModule   = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    SourceEntityId = table.Column<long>(type: "bigint", nullable: true),
                    DetectedAtUtc  = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsResolved     = table.Column<bool>(type: "boolean", nullable: false),
                    ResolvedAtUtc  = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ResolutionNote = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ResolvedByUserId = table.Column<long>(type: "bigint", nullable: true),
                    PublicId    = table.Column<Guid>(type: "uuid", nullable: true),
                    ModifyHost  = table.Column<string>(type: "text", nullable: true),
                    ModifyUserId = table.Column<long>(type: "bigint", nullable: true),
                    ModifyDate  = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreateUserId = table.Column<long>(type: "bigint", nullable: true),
                    CreateHost  = table.Column<string>(type: "text", nullable: true),
                    CreateDate  = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted   = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt   = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy   = table.Column<long>(type: "bigint", nullable: true),
                    IsActive    = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_profile_risk_signals", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_profile_risk_signals_ProfileId_ProfileType_IsResolved",
                schema: "profile",
                table: "profile_risk_signals",
                columns: new[] { "ProfileId", "ProfileType", "IsResolved" });

            migrationBuilder.CreateIndex(
                name: "IX_profile_risk_signals_Severity",
                schema: "profile",
                table: "profile_risk_signals",
                column: "Severity");

            migrationBuilder.CreateIndex(
                name: "IX_profile_risk_signals_SignalCode",
                schema: "profile",
                table: "profile_risk_signals",
                column: "SignalCode");

            // ── profile_metric_caches ─────────────────────────────────────────
            migrationBuilder.CreateTable(
                name: "profile_metric_caches",
                schema: "profile",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProfileId       = table.Column<long>(type: "bigint", nullable: false),
                    ProfileType     = table.Column<int>(type: "integer", nullable: false),
                    MetricKey       = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    MetricValueJson = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    CachedAtUtc  = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExpiresAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PublicId    = table.Column<Guid>(type: "uuid", nullable: true),
                    ModifyHost  = table.Column<string>(type: "text", nullable: true),
                    ModifyUserId = table.Column<long>(type: "bigint", nullable: true),
                    ModifyDate  = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreateUserId = table.Column<long>(type: "bigint", nullable: true),
                    CreateHost  = table.Column<string>(type: "text", nullable: true),
                    CreateDate  = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted   = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt   = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy   = table.Column<long>(type: "bigint", nullable: true),
                    IsActive    = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_profile_metric_caches", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "UIX_profile_metric_caches_ProfileId_ProfileType_MetricKey",
                schema: "profile",
                table: "profile_metric_caches",
                columns: new[] { "ProfileId", "ProfileType", "MetricKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_profile_metric_caches_ExpiresAtUtc",
                schema: "profile",
                table: "profile_metric_caches",
                column: "ExpiresAtUtc");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "profile_metric_caches",         schema: "profile");
            migrationBuilder.DropTable(name: "profile_risk_signals",           schema: "profile");
            migrationBuilder.DropTable(name: "profile_decision_logs",          schema: "profile");
            migrationBuilder.DropTable(name: "profile_score_history",          schema: "profile");
            migrationBuilder.DropTable(name: "profile_score_components",       schema: "profile");
            migrationBuilder.DropTable(name: "profile_performance_snapshots",  schema: "profile");
        }
    }
}
