using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Aizen.Modules.Payment.Repository.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "payment");

            migrationBuilder.CreateTable(
                name: "commission_rules",
                schema: "payment",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RuleType = table.Column<int>(type: "integer", nullable: false),
                    CategoryCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ProviderPlanId = table.Column<long>(type: "bigint", nullable: true),
                    ProviderProfileId = table.Column<long>(type: "bigint", nullable: true),
                    CommissionRate = table.Column<decimal>(type: "numeric(6,4)", nullable: false),
                    EffectiveFrom = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EffectiveTo = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
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
                    table.PrimaryKey("PK_commission_rules", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "invoice_headers",
                schema: "payment",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    InvoiceNumber = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    InvoiceType = table.Column<int>(type: "integer", nullable: false),
                    CommercialModel = table.Column<int>(type: "integer", nullable: false),
                    BillingMode = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    SourceType = table.Column<int>(type: "integer", nullable: false),
                    SourceId = table.Column<long>(type: "bigint", nullable: true),
                    PaymentTransactionId = table.Column<long>(type: "bigint", nullable: true),
                    PaymentReleaseId = table.Column<long>(type: "bigint", nullable: true),
                    CommissionCalculationId = table.Column<long>(type: "bigint", nullable: true),
                    ProviderPayoutId = table.Column<long>(type: "bigint", nullable: true),
                    UserSubscriptionId = table.Column<long>(type: "bigint", nullable: true),
                    OriginalInvoiceId = table.Column<long>(type: "bigint", nullable: true),
                    SellerUserId = table.Column<long>(type: "bigint", nullable: true),
                    SellerName = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    SellerTaxNumber = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    SellerTaxOffice = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    SellerAddress = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    BuyerUserId = table.Column<long>(type: "bigint", nullable: true),
                    BuyerName = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    BuyerTaxNumber = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    BuyerTaxOffice = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    BuyerAddress = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Currency = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    SubTotalAmount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    DiscountAmount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    TaxableAmount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    TaxAmount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    TotalAmount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    PaidAmount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    RemainingAmount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    IssueDateUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DueDateUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PaidAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CancelledAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ExternalInvoiceId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ExternalInvoiceProvider = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    PdfFileRef = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
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
                    table.PrimaryKey("PK_invoice_headers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "invoice_number_sequences",
                schema: "payment",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Prefix = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Year = table.Column<int>(type: "integer", nullable: false),
                    Month = table.Column<int>(type: "integer", nullable: false),
                    LastNumber = table.Column<int>(type: "integer", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PublicId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_invoice_number_sequences", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "participant_plan_subscriptions",
                schema: "payment",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ParticipantProfileId = table.Column<long>(type: "bigint", nullable: false),
                    ParticipantPlanId = table.Column<long>(type: "bigint", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    PaidAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    CurrencyCode = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    SubscriptionPeriodStart = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    SubscriptionPeriodEnd = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AutoRenew = table.Column<bool>(type: "boolean", nullable: false),
                    PaymentTransactionId = table.Column<long>(type: "bigint", nullable: true),
                    ServiceDiscountAtSubscription = table.Column<decimal>(type: "numeric(6,4)", nullable: false),
                    EarnMultiplierAtSubscription = table.Column<decimal>(type: "numeric(6,2)", nullable: false),
                    CancelledAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CancellationReason = table.Column<string>(type: "text", nullable: true),
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
                    table.PrimaryKey("PK_participant_plan_subscriptions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "participant_plans",
                schema: "payment",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PlanCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    MonthlyPriceTRY = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    ServiceDiscountRate = table.Column<decimal>(type: "numeric(6,4)", nullable: false),
                    CargoDryDiscountRate = table.Column<decimal>(type: "numeric(6,4)", nullable: false),
                    InkCoinEarnMultiplier = table.Column<decimal>(type: "numeric(6,2)", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    ValidFrom = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ValidTo = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
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
                    table.PrimaryKey("PK_participant_plans", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "payout_records",
                schema: "payment",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProviderProfileId = table.Column<long>(type: "bigint", nullable: false),
                    PaymentTransactionId = table.Column<long>(type: "bigint", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    CurrencyCode = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    GatewayProvider = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    GatewayPayoutId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    RequestedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ProcessedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    FailureReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    AdminNote = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
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
                    table.PrimaryKey("PK_payout_records", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "provider_payment_profiles",
                schema: "payment",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProviderProfileId = table.Column<long>(type: "bigint", nullable: false),
                    GatewayProvider = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    SubMerchantKey = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    SubMerchantAccountId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    IbanEncrypted = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    LegalName = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    TaxNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    VerifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
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
                    table.PrimaryKey("PK_provider_payment_profiles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "provider_plan_subscriptions",
                schema: "payment",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProviderProfileId = table.Column<long>(type: "bigint", nullable: false),
                    ProviderPlanId = table.Column<long>(type: "bigint", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    PaidAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    CurrencyCode = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    SubscriptionPeriodStart = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    SubscriptionPeriodEnd = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AutoRenew = table.Column<bool>(type: "boolean", nullable: false),
                    PaymentTransactionId = table.Column<long>(type: "bigint", nullable: true),
                    CommissionRateAtSubscription = table.Column<decimal>(type: "numeric(6,4)", nullable: false),
                    CancelledAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CancellationReason = table.Column<string>(type: "text", nullable: true),
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
                    table.PrimaryKey("PK_provider_plan_subscriptions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "provider_plans",
                schema: "payment",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PlanCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    MonthlyPriceTRY = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    MaxActiveOffers = table.Column<int>(type: "integer", nullable: true),
                    HasPriorityBoost = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    HasFullAnalytics = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    ValidFrom = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ValidTo = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
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
                    table.PrimaryKey("PK_provider_plans", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "transactions",
                schema: "payment",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TransactionCode = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    TransactionType = table.Column<int>(type: "integer", nullable: false),
                    ContextType = table.Column<int>(type: "integer", nullable: false),
                    ContextId = table.Column<long>(type: "bigint", nullable: false),
                    ContextSubId = table.Column<long>(type: "bigint", nullable: true),
                    PayerProfileId = table.Column<long>(type: "bigint", nullable: false),
                    RecipientProfileId = table.Column<long>(type: "bigint", nullable: true),
                    GrossAmount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    CommissionAmount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    CommissionRateSnapshot = table.Column<decimal>(type: "numeric(6,4)", nullable: false),
                    VatOnCommission = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    NetPayoutAmount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    DiscountAmount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    CurrencyCode = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    TotalRefundedAmount = table.Column<decimal>(type: "numeric(18,4)", nullable: false, defaultValue: 0m),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    GatewayProvider = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    GatewayReference = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    IdempotencyKey = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    EscrowRequired = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    CapturedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ReleasedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CancelledAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CancellationReason = table.Column<int>(type: "integer", nullable: true),
                    ReinstatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ReinstationNote = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    LastRefundedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DisputedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DisputeResolution = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    AdminNote = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
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
                    table.PrimaryKey("PK_transactions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "invoice_external_integrations",
                schema: "payment",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    InvoiceHeaderId = table.Column<long>(type: "bigint", nullable: false),
                    Provider = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ExternalInvoiceId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ErrorMessage = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    RawRequestPayload = table.Column<string>(type: "text", nullable: true),
                    RawResponsePayload = table.Column<string>(type: "text", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PublicId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_invoice_external_integrations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_invoice_external_integrations_invoice_headers_InvoiceHeader~",
                        column: x => x.InvoiceHeaderId,
                        principalSchema: "payment",
                        principalTable: "invoice_headers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "invoice_lines",
                schema: "payment",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    InvoiceHeaderId = table.Column<long>(type: "bigint", nullable: false),
                    LineNumber = table.Column<int>(type: "integer", nullable: false),
                    LineType = table.Column<int>(type: "integer", nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ProductCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    ServiceCategoryCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Quantity = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    UnitCode = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    UnitPrice = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    LineSubTotal = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    DiscountAmount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    TaxRate = table.Column<decimal>(type: "numeric(6,4)", nullable: false),
                    TaxAmount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    LineTotal = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    SourceType = table.Column<int>(type: "integer", nullable: true),
                    SourceId = table.Column<long>(type: "bigint", nullable: true),
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
                    table.PrimaryKey("PK_invoice_lines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_invoice_lines_invoice_headers_InvoiceHeaderId",
                        column: x => x.InvoiceHeaderId,
                        principalSchema: "payment",
                        principalTable: "invoice_headers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "invoice_status_history",
                schema: "payment",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    InvoiceHeaderId = table.Column<long>(type: "bigint", nullable: false),
                    OldStatus = table.Column<int>(type: "integer", nullable: false),
                    NewStatus = table.Column<int>(type: "integer", nullable: false),
                    Reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ChangedByUserId = table.Column<long>(type: "bigint", nullable: true),
                    ChangedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PublicId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_invoice_status_history", x => x.Id);
                    table.ForeignKey(
                        name: "FK_invoice_status_history_invoice_headers_InvoiceHeaderId",
                        column: x => x.InvoiceHeaderId,
                        principalSchema: "payment",
                        principalTable: "invoice_headers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "invoice_tax_breakdowns",
                schema: "payment",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    InvoiceHeaderId = table.Column<long>(type: "bigint", nullable: false),
                    TaxType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    TaxRate = table.Column<decimal>(type: "numeric(6,4)", nullable: false),
                    TaxableAmount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    TaxAmount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
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
                    table.PrimaryKey("PK_invoice_tax_breakdowns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_invoice_tax_breakdowns_invoice_headers_InvoiceHeaderId",
                        column: x => x.InvoiceHeaderId,
                        principalSchema: "payment",
                        principalTable: "invoice_headers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "transaction_refund_records",
                schema: "payment",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PaymentTransactionId = table.Column<long>(type: "bigint", nullable: false),
                    RefundCode = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    RefundType = table.Column<int>(type: "integer", nullable: false),
                    Reason = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    CurrencyCode = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    GatewayRefundReference = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ProcessedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    FailureReason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    AdminNote = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ReversedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ReversalReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ReversalAdminNote = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
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
                    table.PrimaryKey("PK_transaction_refund_records", x => x.Id);
                    table.ForeignKey(
                        name: "FK_transaction_refund_records_transactions_PaymentTransactionId",
                        column: x => x.PaymentTransactionId,
                        principalSchema: "payment",
                        principalTable: "transactions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_commission_rules_RuleType",
                schema: "payment",
                table: "commission_rules",
                column: "RuleType");

            migrationBuilder.CreateIndex(
                name: "IX_invoice_external_integrations_InvoiceHeaderId",
                schema: "payment",
                table: "invoice_external_integrations",
                column: "InvoiceHeaderId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_invoice_external_integrations_Status",
                schema: "payment",
                table: "invoice_external_integrations",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_invoice_headers_BuyerUserId",
                schema: "payment",
                table: "invoice_headers",
                column: "BuyerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_invoice_headers_InvoiceNumber",
                schema: "payment",
                table: "invoice_headers",
                column: "InvoiceNumber",
                unique: true,
                filter: "invoice_number IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_invoice_headers_InvoiceType",
                schema: "payment",
                table: "invoice_headers",
                column: "InvoiceType");

            migrationBuilder.CreateIndex(
                name: "IX_invoice_headers_OriginalInvoiceId",
                schema: "payment",
                table: "invoice_headers",
                column: "OriginalInvoiceId",
                filter: "original_invoice_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_invoice_headers_PaymentTransactionId",
                schema: "payment",
                table: "invoice_headers",
                column: "PaymentTransactionId",
                filter: "payment_transaction_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_invoice_headers_Status",
                schema: "payment",
                table: "invoice_headers",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_invoice_headers_Status_DueDateUtc",
                schema: "payment",
                table: "invoice_headers",
                columns: new[] { "Status", "DueDateUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_invoice_lines_InvoiceHeaderId",
                schema: "payment",
                table: "invoice_lines",
                column: "InvoiceHeaderId");

            migrationBuilder.CreateIndex(
                name: "IX_invoice_lines_InvoiceHeaderId_LineNumber",
                schema: "payment",
                table: "invoice_lines",
                columns: new[] { "InvoiceHeaderId", "LineNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_invoice_number_sequences_Prefix_Year_Month",
                schema: "payment",
                table: "invoice_number_sequences",
                columns: new[] { "Prefix", "Year", "Month" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_invoice_status_history_ChangedAtUtc",
                schema: "payment",
                table: "invoice_status_history",
                column: "ChangedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_invoice_status_history_InvoiceHeaderId",
                schema: "payment",
                table: "invoice_status_history",
                column: "InvoiceHeaderId");

            migrationBuilder.CreateIndex(
                name: "IX_invoice_tax_breakdowns_InvoiceHeaderId",
                schema: "payment",
                table: "invoice_tax_breakdowns",
                column: "InvoiceHeaderId");

            migrationBuilder.CreateIndex(
                name: "IX_invoice_tax_breakdowns_InvoiceHeaderId_TaxType",
                schema: "payment",
                table: "invoice_tax_breakdowns",
                columns: new[] { "InvoiceHeaderId", "TaxType" });

            migrationBuilder.CreateIndex(
                name: "IX_participant_plan_subscriptions_ParticipantProfileId_Status",
                schema: "payment",
                table: "participant_plan_subscriptions",
                columns: new[] { "ParticipantProfileId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_participant_plans_PlanCode",
                schema: "payment",
                table: "participant_plans",
                column: "PlanCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_payout_records_ProviderProfileId",
                schema: "payment",
                table: "payout_records",
                column: "ProviderProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_payout_records_Status",
                schema: "payment",
                table: "payout_records",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_provider_payment_profiles_ProviderProfileId",
                schema: "payment",
                table: "provider_payment_profiles",
                column: "ProviderProfileId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_provider_plan_subscriptions_ProviderProfileId_Status",
                schema: "payment",
                table: "provider_plan_subscriptions",
                columns: new[] { "ProviderProfileId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_provider_plans_PlanCode",
                schema: "payment",
                table: "provider_plans",
                column: "PlanCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_transaction_refund_records_PaymentTransactionId",
                schema: "payment",
                table: "transaction_refund_records",
                column: "PaymentTransactionId");

            migrationBuilder.CreateIndex(
                name: "IX_transaction_refund_records_RefundCode",
                schema: "payment",
                table: "transaction_refund_records",
                column: "RefundCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_transaction_refund_records_Status",
                schema: "payment",
                table: "transaction_refund_records",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_transactions_ContextType_ContextId",
                schema: "payment",
                table: "transactions",
                columns: new[] { "ContextType", "ContextId" });

            migrationBuilder.CreateIndex(
                name: "IX_transactions_IdempotencyKey",
                schema: "payment",
                table: "transactions",
                column: "IdempotencyKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_transactions_PayerProfileId",
                schema: "payment",
                table: "transactions",
                column: "PayerProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_transactions_Status",
                schema: "payment",
                table: "transactions",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_transactions_TransactionCode",
                schema: "payment",
                table: "transactions",
                column: "TransactionCode",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "commission_rules",
                schema: "payment");

            migrationBuilder.DropTable(
                name: "invoice_external_integrations",
                schema: "payment");

            migrationBuilder.DropTable(
                name: "invoice_lines",
                schema: "payment");

            migrationBuilder.DropTable(
                name: "invoice_number_sequences",
                schema: "payment");

            migrationBuilder.DropTable(
                name: "invoice_status_history",
                schema: "payment");

            migrationBuilder.DropTable(
                name: "invoice_tax_breakdowns",
                schema: "payment");

            migrationBuilder.DropTable(
                name: "participant_plan_subscriptions",
                schema: "payment");

            migrationBuilder.DropTable(
                name: "participant_plans",
                schema: "payment");

            migrationBuilder.DropTable(
                name: "payout_records",
                schema: "payment");

            migrationBuilder.DropTable(
                name: "provider_payment_profiles",
                schema: "payment");

            migrationBuilder.DropTable(
                name: "provider_plan_subscriptions",
                schema: "payment");

            migrationBuilder.DropTable(
                name: "provider_plans",
                schema: "payment");

            migrationBuilder.DropTable(
                name: "transaction_refund_records",
                schema: "payment");

            migrationBuilder.DropTable(
                name: "invoice_headers",
                schema: "payment");

            migrationBuilder.DropTable(
                name: "transactions",
                schema: "payment");
        }
    }
}
