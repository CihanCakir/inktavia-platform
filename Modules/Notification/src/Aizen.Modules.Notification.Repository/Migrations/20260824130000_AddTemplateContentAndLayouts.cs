using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Aizen.Modules.Notification.Repository.Migrations
{
    /// <inheritdoc />
    public partial class AddTemplateContentAndLayouts : Migration
    {
        // Minimal Inktavia DEFAULT layout kabuğu — DefaultEmailLayoutSeed.HtmlShell ile AYNI olmalı (taze DB seed'i ile
        // mevcut DB migration'ı tutarlı kalsın diye). {{content}} render edilen gövdeyle değişir.
        private const string DefaultLayoutHtmlShell =
            "<!DOCTYPE html><html lang=\"en\"><head><meta charset=\"utf-8\">" +
            "<meta name=\"viewport\" content=\"width=device-width,initial-scale=1\"></head>" +
            "<body style=\"margin:0;background:#f4f5f7;font-family:Arial,Helvetica,sans-serif;color:#1a1a1a;\">" +
            "<table role=\"presentation\" width=\"100%\" cellpadding=\"0\" cellspacing=\"0\" style=\"background:#f4f5f7;padding:24px 0;\">" +
            "<tr><td align=\"center\">" +
            "<table role=\"presentation\" width=\"600\" cellpadding=\"0\" cellspacing=\"0\" style=\"max-width:600px;background:#ffffff;border-radius:8px;overflow:hidden;\">" +
            "<tr><td style=\"background:#0b3d5c;padding:20px 24px;color:#ffffff;font-size:18px;font-weight:bold;\">Inktavia Marine</td></tr>" +
            "<tr><td style=\"padding:24px;font-size:15px;line-height:1.5;\">{{content}}</td></tr>" +
            "<tr><td style=\"padding:16px 24px;background:#f0f1f3;color:#8a8f98;font-size:12px;\">&#169; Inktavia Marine</td></tr>" +
            "</table></td></tr></table></body></html>";

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ── Yeni kolonlar ──────────────────────────────────────────────
            migrationBuilder.AddColumn<string>(
                name: "Description", schema: "notification", table: "notification_templates",
                type: "character varying(1000)", maxLength: 1000, nullable: true);

            // Locale NOT NULL: önce nullable ekle → 'en' geriye doldur → NOT NULL'a çevir (kalıcı DB default bırakmadan).
            migrationBuilder.AddColumn<string>(
                name: "Locale", schema: "notification", table: "notifications",
                type: "character varying(8)", maxLength: 8, nullable: true);
            migrationBuilder.Sql(@"UPDATE notification.notifications SET ""Locale"" = 'en' WHERE ""Locale"" IS NULL;");
            migrationBuilder.AlterColumn<string>(
                name: "Locale", schema: "notification", table: "notifications",
                type: "character varying(8)", maxLength: 8, nullable: false,
                oldClrType: typeof(string), oldType: "character varying(8)", oldMaxLength: 8, oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeepLink", schema: "notification", table: "notifications",
                type: "character varying(1000)", maxLength: 1000, nullable: true);

            // ── email_layouts ─────────────────────────────────────────────
            migrationBuilder.CreateTable(
                name: "email_layouts",
                schema: "notification",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    HtmlShell = table.Column<string>(type: "character varying(20000)", maxLength: 20000, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    PublicId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_email_layouts", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_email_layouts_Code",
                schema: "notification",
                table: "email_layouts",
                column: "Code",
                unique: true);

            // ── notification_template_contents ────────────────────────────
            migrationBuilder.CreateTable(
                name: "notification_template_contents",
                schema: "notification",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TemplateId = table.Column<long>(type: "bigint", nullable: false),
                    Channel = table.Column<int>(type: "integer", nullable: false),
                    Locale = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    SubjectTemplate = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    HtmlTemplate = table.Column<string>(type: "character varying(20000)", maxLength: 20000, nullable: true),
                    TextTemplate = table.Column<string>(type: "character varying(8000)", maxLength: 8000, nullable: true),
                    TitleTemplate = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    BodyTemplate = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    DeepLinkTemplate = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    SmsTextTemplate = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    LayoutCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    PublicId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_notification_template_contents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_notification_template_contents_templates_TemplateId",
                        column: x => x.TemplateId,
                        principalSchema: "notification",
                        principalTable: "notification_templates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ntc_TemplateId_Channel_Status",
                schema: "notification",
                table: "notification_template_contents",
                columns: new[] { "TemplateId", "Channel", "Status" });

            migrationBuilder.CreateIndex(
                name: "UX_ntc_TemplateId_Channel_Locale_Version",
                schema: "notification",
                table: "notification_template_contents",
                columns: new[] { "TemplateId", "Channel", "Locale", "Version" },
                unique: true);

            // ── DEFAULT e-posta layout'u (idempotent) ─────────────────────
            migrationBuilder.Sql($@"
                INSERT INTO notification.email_layouts (""Code"", ""Name"", ""HtmlShell"", ""IsActive"", ""CreatedAt"", ""IsDeleted"")
                SELECT 'DEFAULT', 'Inktavia Default', '{DefaultLayoutHtmlShell}', true, now(), false
                WHERE NOT EXISTS (SELECT 1 FROM notification.email_layouts WHERE ""Code"" = 'DEFAULT');
            ");

            // ── Mevcut template içeriklerini içerik satırlarına TAŞI ──────
            // Her template'in Title/Body'si 'en' v1 Published içerik satırına gider. Email kanalında ayrıca
            // Subject=Title, Html=Body, Layout='DEFAULT'. Böylece migration sonrası üretim davranışı (özellikle InApp)
            // birebir korunur (golden test doğrular). EXISTS guard idempotenttir (taze DB'de template yoktur → no-op;
            // seed taze DB'de içeriği ayrıca üretir). DefaultTemplateContent.FromTemplate ile AYNI eşleme.
            migrationBuilder.Sql(@"
                INSERT INTO notification.notification_template_contents
                    (""TemplateId"", ""Channel"", ""Locale"", ""Version"", ""Status"",
                     ""TitleTemplate"", ""BodyTemplate"", ""SubjectTemplate"", ""HtmlTemplate"", ""LayoutCode"",
                     ""CreatedAt"", ""IsDeleted"")
                SELECT t.""Id"", t.""Channel"", 'en', 1, 1,
                       t.""TitleTemplate"", t.""BodyTemplate"",
                       CASE WHEN t.""Channel"" = 3 THEN t.""TitleTemplate"" ELSE NULL END,
                       CASE WHEN t.""Channel"" = 3 THEN t.""BodyTemplate""  ELSE NULL END,
                       CASE WHEN t.""Channel"" = 3 THEN 'DEFAULT' ELSE NULL END,
                       now(), false
                FROM notification.notification_templates t
                WHERE t.""IsDeleted"" = false
                  AND NOT EXISTS (
                      SELECT 1 FROM notification.notification_template_contents c
                      WHERE c.""TemplateId"" = t.""Id"" AND c.""Channel"" = t.""Channel"");
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "notification_template_contents",
                schema: "notification");

            migrationBuilder.DropTable(
                name: "email_layouts",
                schema: "notification");

            migrationBuilder.DropColumn(name: "DeepLink", schema: "notification", table: "notifications");
            migrationBuilder.DropColumn(name: "Locale", schema: "notification", table: "notifications");
            migrationBuilder.DropColumn(name: "Description", schema: "notification", table: "notification_templates");
        }
    }
}
