using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E_POS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddFlow4ManualPaymentRuntime : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_onboarding_operations_invitation_status",
                table: "platform_tenant_onboarding_operations");

            migrationBuilder.DropCheckConstraint(
                name: "ck_onboarding_operations_payment_status",
                table: "platform_tenant_onboarding_operations");

            migrationBuilder.AlterColumn<Guid>(
                name: "subscription_payment_link_id",
                table: "subscription_payment_transactions",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<string>(
                name: "provider_transaction_id",
                table: "subscription_payment_transactions",
                type: "varchar(150)",
                maxLength: 150,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(150)",
                oldMaxLength: 150);

            migrationBuilder.AddColumn<decimal>(
                name: "approved_amount",
                table: "subscription_payment_transactions",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "expected_amount",
                table: "subscription_payment_transactions",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "failure_code",
                table: "subscription_payment_transactions",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "last_command_idempotency_key_hash",
                table: "subscription_payment_transactions",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "last_command_request_hash",
                table: "subscription_payment_transactions",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "manual_reference",
                table: "subscription_payment_transactions",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "manual_reference_normalized",
                table: "subscription_payment_transactions",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "payer_note",
                table: "subscription_payment_transactions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "payment_date",
                table: "subscription_payment_transactions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "payment_method",
                table: "subscription_payment_transactions",
                type: "character varying(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "provider_callback_receipt_json",
                table: "subscription_payment_transactions",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "provider_checkout_url",
                table: "subscription_payment_transactions",
                type: "character varying(700)",
                maxLength: 700,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "provider_customer_reference_id",
                table: "subscription_payment_transactions",
                type: "character varying(180)",
                maxLength: 180,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "provider_event_id",
                table: "subscription_payment_transactions",
                type: "character varying(180)",
                maxLength: 180,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "provider_status",
                table: "subscription_payment_transactions",
                type: "character varying(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "rejection_reason_code",
                table: "subscription_payment_transactions",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "review_note",
                table: "subscription_payment_transactions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "submission_version",
                table: "subscription_payment_transactions",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<decimal>(
                name: "submitted_amount",
                table: "subscription_payment_transactions",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "submitted_at",
                table: "subscription_payment_transactions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "submitted_by_id",
                table: "subscription_payment_transactions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "submitted_by_type",
                table: "subscription_payment_transactions",
                type: "character varying(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "tenant_subscription_id",
                table: "subscription_payment_transactions",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "verified_at",
                table: "subscription_payment_transactions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "verified_by_platform_user_id",
                table: "subscription_payment_transactions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "version",
                table: "subscription_payment_transactions",
                type: "bigint",
                nullable: false,
                defaultValue: 1L);

            migrationBuilder.AlterColumn<string>(
                name: "token_hash",
                table: "subscription_payment_links",
                type: "varchar(255)",
                maxLength: 255,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(255)",
                oldMaxLength: 255);

            migrationBuilder.AlterColumn<string>(
                name: "payment_url",
                table: "subscription_payment_links",
                type: "varchar(700)",
                maxLength: 700,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(700)",
                oldMaxLength: 700);

            migrationBuilder.AlterColumn<string>(
                name: "payment_link_token_hash",
                table: "subscription_payment_links",
                type: "varchar(255)",
                maxLength: 255,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(255)",
                oldMaxLength: 255);

            migrationBuilder.AddColumn<string>(
                name: "allowed_actions",
                table: "subscription_payment_links",
                type: "character varying(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "last_accessed_at",
                table: "subscription_payment_links",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "payment_transaction_id",
                table: "subscription_payment_links",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "purpose",
                table: "subscription_payment_links",
                type: "character varying(60)",
                maxLength: 60,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "recipient_identifier_hash",
                table: "subscription_payment_links",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "recipient_type",
                table: "subscription_payment_links",
                type: "character varying(60)",
                maxLength: 60,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "token_provisioned_at",
                table: "subscription_payment_links",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "version",
                table: "subscription_payment_links",
                type: "bigint",
                nullable: false,
                defaultValue: 1L);

            // Backfill newly required relationships and semantics for legacy provider rows,
            // then remove temporary defaults so future writes must supply canonical values.
            migrationBuilder.Sql("""
                UPDATE subscription_payment_transactions AS payment
                SET expected_amount = payment.amount,
                    tenant_subscription_id = invoice.tenant_subscription_id
                FROM subscription_invoices AS invoice
                WHERE invoice.id = payment.invoice_id;

                UPDATE subscription_payment_links
                SET purpose = 'LEGACY_PROVIDER_PAYMENT',
                    allowed_actions = 'STATUS',
                    recipient_type = 'LEGACY_RECIPIENT';

                ALTER TABLE subscription_payment_transactions ALTER COLUMN expected_amount DROP DEFAULT;
                ALTER TABLE subscription_payment_transactions ALTER COLUMN tenant_subscription_id DROP DEFAULT;
                ALTER TABLE subscription_payment_links ALTER COLUMN purpose DROP DEFAULT;
                ALTER TABLE subscription_payment_links ALTER COLUMN allowed_actions DROP DEFAULT;
                ALTER TABLE subscription_payment_links ALTER COLUMN recipient_type DROP DEFAULT;
                """);

            migrationBuilder.CreateTable(
                name: "subscription_payment_evidence",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    payment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    invoice_id = table.Column<Guid>(type: "uuid", nullable: false),
                    blob_container = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    storage_key = table.Column<string>(type: "character varying(700)", maxLength: 700, nullable: false),
                    original_file_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    safe_file_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    content_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    file_size = table.Column<long>(type: "bigint", nullable: false),
                    sha256 = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    evidence_type = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    uploaded_by_type = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    uploaded_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    submission_version = table.Column<long>(type: "bigint", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    superseded_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    scan_status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    scan_failure_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_subscription_payment_evidence", x => x.id);
                    table.CheckConstraint("ck_subscription_payment_evidence_file_size", "file_size > 0");
                    table.ForeignKey(
                        name: "fk_subscription_payment_evidence_invoice",
                        column: x => x.invoice_id,
                        principalTable: "subscription_invoices",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_subscription_payment_evidence_payment",
                        column: x => x.payment_id,
                        principalTable: "subscription_payment_transactions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_subscription_payment_evidence_tenant",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "subscription_payment_reviews",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    payment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    invoice_id = table.Column<Guid>(type: "uuid", nullable: false),
                    action = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    status_before = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    status_after = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    actor_type = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    actor_id = table.Column<Guid>(type: "uuid", nullable: true),
                    review_note = table.Column<string>(type: "text", nullable: true),
                    reason_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    idempotency_key_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    request_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    correlation_id = table.Column<Guid>(type: "uuid", nullable: false),
                    payment_version = table.Column<long>(type: "bigint", nullable: false),
                    submitted_amount_snapshot = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    expected_amount_snapshot = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    currency_snapshot = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    evidence_id_snapshot = table.Column<Guid>(type: "uuid", nullable: true),
                    evidence_version_snapshot = table.Column<long>(type: "bigint", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_subscription_payment_reviews", x => x.id);
                    table.ForeignKey(
                        name: "fk_subscription_payment_reviews_actor",
                        column: x => x.actor_id,
                        principalTable: "platform_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_subscription_payment_reviews_invoice",
                        column: x => x.invoice_id,
                        principalTable: "subscription_invoices",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_subscription_payment_reviews_payment",
                        column: x => x.payment_id,
                        principalTable: "subscription_payment_transactions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_subscription_payment_reviews_tenant",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_subscription_payment_transactions_manual_reference",
                table: "subscription_payment_transactions",
                columns: new[] { "tenant_id", "invoice_id", "manual_reference_normalized" },
                filter: "manual_reference_normalized IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_subscription_payment_transactions_tenant_subscription_id",
                table: "subscription_payment_transactions",
                column: "tenant_subscription_id");

            migrationBuilder.CreateIndex(
                name: "uq_subscription_payment_transactions_provider_event",
                table: "subscription_payment_transactions",
                columns: new[] { "provider_name", "provider_event_id" },
                unique: true,
                filter: "provider_event_id IS NOT NULL");

            migrationBuilder.AddCheckConstraint(
                name: "ck_subscription_payment_transactions_expected_amount",
                table: "subscription_payment_transactions",
                sql: "expected_amount >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_subscription_payment_transactions_submitted_amount",
                table: "subscription_payment_transactions",
                sql: "submitted_amount IS NULL OR submitted_amount > 0");

            migrationBuilder.CreateIndex(
                name: "ix_subscription_payment_links_payment_transaction_id",
                table: "subscription_payment_links",
                column: "payment_transaction_id");

            migrationBuilder.CreateIndex(
                name: "uq_subscription_payment_links_active_purpose",
                table: "subscription_payment_links",
                columns: new[] { "payment_transaction_id", "purpose" },
                unique: true,
                filter: "payment_transaction_id IS NOT NULL AND revoked_at IS NULL AND link_status IN ('PENDING_DELIVERY','ACTIVE')");

            migrationBuilder.CreateIndex(
                name: "uq_subscription_payment_links_token_hash",
                table: "subscription_payment_links",
                column: "token_hash",
                unique: true,
                filter: "token_hash IS NOT NULL");

            migrationBuilder.AddCheckConstraint(
                name: "ck_onboarding_operations_invitation_status",
                table: "platform_tenant_onboarding_operations",
                sql: "invitation_status IN ('NOT_ELIGIBLE','PENDING_ACTIVATION','PENDING','SENT','FAILED','ACCEPTED','EXPIRED')");

            migrationBuilder.AddCheckConstraint(
                name: "ck_onboarding_operations_payment_status",
                table: "platform_tenant_onboarding_operations",
                sql: "payment_status IN ('NOT_REQUIRED','PENDING','CONFIRMED','FAILED','WAIVED','AWAITING_PAYMENT','PAYMENT_SUBMITTED','UNDER_REVIEW','ACTION_REQUIRED','PAID','REJECTED','EXPIRED','CANCELLED','DEFERRED')");

            migrationBuilder.CreateIndex(
                name: "IX_subscription_payment_evidence_invoice_id",
                table: "subscription_payment_evidence",
                column: "invoice_id");

            migrationBuilder.CreateIndex(
                name: "ix_subscription_payment_evidence_payment_submission",
                table: "subscription_payment_evidence",
                columns: new[] { "payment_id", "submission_version" });

            migrationBuilder.CreateIndex(
                name: "IX_subscription_payment_evidence_tenant_id",
                table: "subscription_payment_evidence",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "uq_subscription_payment_evidence_storage_key",
                table: "subscription_payment_evidence",
                column: "storage_key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_subscription_payment_reviews_actor_id",
                table: "subscription_payment_reviews",
                column: "actor_id");

            migrationBuilder.CreateIndex(
                name: "IX_subscription_payment_reviews_invoice_id",
                table: "subscription_payment_reviews",
                column: "invoice_id");

            migrationBuilder.CreateIndex(
                name: "ix_subscription_payment_reviews_payment_created",
                table: "subscription_payment_reviews",
                columns: new[] { "payment_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "IX_subscription_payment_reviews_tenant_id",
                table: "subscription_payment_reviews",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "uq_subscription_payment_reviews_payment_idempotency",
                table: "subscription_payment_reviews",
                columns: new[] { "payment_id", "idempotency_key_hash" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "fk_subscription_payment_links_payment_transaction_id",
                table: "subscription_payment_links",
                column: "payment_transaction_id",
                principalTable: "subscription_payment_transactions",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_subscription_payment_transactions_tenant_subscription_id",
                table: "subscription_payment_transactions",
                column: "tenant_subscription_id",
                principalTable: "tenant_subscriptions",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_subscription_payment_links_payment_transaction_id",
                table: "subscription_payment_links");

            migrationBuilder.DropForeignKey(
                name: "fk_subscription_payment_transactions_tenant_subscription_id",
                table: "subscription_payment_transactions");

            migrationBuilder.DropTable(
                name: "subscription_payment_evidence");

            migrationBuilder.DropTable(
                name: "subscription_payment_reviews");

            migrationBuilder.DropIndex(
                name: "ix_subscription_payment_transactions_manual_reference",
                table: "subscription_payment_transactions");

            migrationBuilder.DropIndex(
                name: "ix_subscription_payment_transactions_tenant_subscription_id",
                table: "subscription_payment_transactions");

            migrationBuilder.DropIndex(
                name: "uq_subscription_payment_transactions_provider_event",
                table: "subscription_payment_transactions");

            migrationBuilder.DropCheckConstraint(
                name: "ck_subscription_payment_transactions_expected_amount",
                table: "subscription_payment_transactions");

            migrationBuilder.DropCheckConstraint(
                name: "ck_subscription_payment_transactions_submitted_amount",
                table: "subscription_payment_transactions");

            migrationBuilder.DropIndex(
                name: "ix_subscription_payment_links_payment_transaction_id",
                table: "subscription_payment_links");

            migrationBuilder.DropIndex(
                name: "uq_subscription_payment_links_active_purpose",
                table: "subscription_payment_links");

            migrationBuilder.DropIndex(
                name: "uq_subscription_payment_links_token_hash",
                table: "subscription_payment_links");

            migrationBuilder.DropCheckConstraint(
                name: "ck_onboarding_operations_invitation_status",
                table: "platform_tenant_onboarding_operations");

            migrationBuilder.DropCheckConstraint(
                name: "ck_onboarding_operations_payment_status",
                table: "platform_tenant_onboarding_operations");

            migrationBuilder.DropColumn(
                name: "approved_amount",
                table: "subscription_payment_transactions");

            migrationBuilder.DropColumn(
                name: "expected_amount",
                table: "subscription_payment_transactions");

            migrationBuilder.DropColumn(
                name: "failure_code",
                table: "subscription_payment_transactions");

            migrationBuilder.DropColumn(
                name: "last_command_idempotency_key_hash",
                table: "subscription_payment_transactions");

            migrationBuilder.DropColumn(
                name: "last_command_request_hash",
                table: "subscription_payment_transactions");

            migrationBuilder.DropColumn(
                name: "manual_reference",
                table: "subscription_payment_transactions");

            migrationBuilder.DropColumn(
                name: "manual_reference_normalized",
                table: "subscription_payment_transactions");

            migrationBuilder.DropColumn(
                name: "payer_note",
                table: "subscription_payment_transactions");

            migrationBuilder.DropColumn(
                name: "payment_date",
                table: "subscription_payment_transactions");

            migrationBuilder.DropColumn(
                name: "payment_method",
                table: "subscription_payment_transactions");

            migrationBuilder.DropColumn(
                name: "provider_callback_receipt_json",
                table: "subscription_payment_transactions");

            migrationBuilder.DropColumn(
                name: "provider_checkout_url",
                table: "subscription_payment_transactions");

            migrationBuilder.DropColumn(
                name: "provider_customer_reference_id",
                table: "subscription_payment_transactions");

            migrationBuilder.DropColumn(
                name: "provider_event_id",
                table: "subscription_payment_transactions");

            migrationBuilder.DropColumn(
                name: "provider_status",
                table: "subscription_payment_transactions");

            migrationBuilder.DropColumn(
                name: "rejection_reason_code",
                table: "subscription_payment_transactions");

            migrationBuilder.DropColumn(
                name: "review_note",
                table: "subscription_payment_transactions");

            migrationBuilder.DropColumn(
                name: "submission_version",
                table: "subscription_payment_transactions");

            migrationBuilder.DropColumn(
                name: "submitted_amount",
                table: "subscription_payment_transactions");

            migrationBuilder.DropColumn(
                name: "submitted_at",
                table: "subscription_payment_transactions");

            migrationBuilder.DropColumn(
                name: "submitted_by_id",
                table: "subscription_payment_transactions");

            migrationBuilder.DropColumn(
                name: "submitted_by_type",
                table: "subscription_payment_transactions");

            migrationBuilder.DropColumn(
                name: "tenant_subscription_id",
                table: "subscription_payment_transactions");

            migrationBuilder.DropColumn(
                name: "verified_at",
                table: "subscription_payment_transactions");

            migrationBuilder.DropColumn(
                name: "verified_by_platform_user_id",
                table: "subscription_payment_transactions");

            migrationBuilder.DropColumn(
                name: "version",
                table: "subscription_payment_transactions");

            migrationBuilder.DropColumn(
                name: "allowed_actions",
                table: "subscription_payment_links");

            migrationBuilder.DropColumn(
                name: "last_accessed_at",
                table: "subscription_payment_links");

            migrationBuilder.DropColumn(
                name: "payment_transaction_id",
                table: "subscription_payment_links");

            migrationBuilder.DropColumn(
                name: "purpose",
                table: "subscription_payment_links");

            migrationBuilder.DropColumn(
                name: "recipient_identifier_hash",
                table: "subscription_payment_links");

            migrationBuilder.DropColumn(
                name: "recipient_type",
                table: "subscription_payment_links");

            migrationBuilder.DropColumn(
                name: "token_provisioned_at",
                table: "subscription_payment_links");

            migrationBuilder.DropColumn(
                name: "version",
                table: "subscription_payment_links");

            migrationBuilder.AlterColumn<Guid>(
                name: "subscription_payment_link_id",
                table: "subscription_payment_transactions",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "provider_transaction_id",
                table: "subscription_payment_transactions",
                type: "varchar(150)",
                maxLength: 150,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "varchar(150)",
                oldMaxLength: 150,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "token_hash",
                table: "subscription_payment_links",
                type: "varchar(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "varchar(255)",
                oldMaxLength: 255,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "payment_url",
                table: "subscription_payment_links",
                type: "varchar(700)",
                maxLength: 700,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "varchar(700)",
                oldMaxLength: 700,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "payment_link_token_hash",
                table: "subscription_payment_links",
                type: "varchar(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "varchar(255)",
                oldMaxLength: 255,
                oldNullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_onboarding_operations_invitation_status",
                table: "platform_tenant_onboarding_operations",
                sql: "invitation_status IN ('NOT_ELIGIBLE','PENDING','SENT','FAILED','ACCEPTED','EXPIRED')");

            migrationBuilder.AddCheckConstraint(
                name: "ck_onboarding_operations_payment_status",
                table: "platform_tenant_onboarding_operations",
                sql: "payment_status IN ('NOT_REQUIRED','PENDING','CONFIRMED','FAILED','WAIVED')");
        }
    }
}
