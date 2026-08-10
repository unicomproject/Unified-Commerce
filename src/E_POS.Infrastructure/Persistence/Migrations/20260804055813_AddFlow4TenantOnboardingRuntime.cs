using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E_POS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddFlow4TenantOnboardingRuntime : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "billing_cycle",
                table: "tenant_subscriptions",
                type: "varchar(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(20)",
                oldMaxLength: 20,
                oldDefaultValue: "monthly");

            migrationBuilder.AddColumn<string>(
                name: "registration_number",
                table: "tenant_profiles",
                type: "varchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "tax_number",
                table: "tenant_profiles",
                type: "varchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "override_reason",
                table: "tenant_feature_entitlements",
                type: "varchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "integration_outbox_messages",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    message_type = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: false),
                    aggregate_type = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: false),
                    aggregate_id = table.Column<Guid>(type: "uuid", nullable: false),
                    aggregate_sequence = table.Column<long>(type: "bigint", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: true),
                    correlation_id = table.Column<Guid>(type: "uuid", nullable: false),
                    causation_id = table.Column<Guid>(type: "uuid", nullable: true),
                    payload_json = table.Column<string>(type: "jsonb", nullable: false),
                    payload_schema_version = table.Column<int>(type: "integer", nullable: false),
                    deduplication_key = table.Column<string>(type: "varchar(180)", maxLength: 180, nullable: false),
                    status = table.Column<string>(type: "varchar(24)", maxLength: 24, nullable: false),
                    attempt_count = table.Column<int>(type: "integer", nullable: false),
                    available_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    lease_owner = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: true),
                    lease_expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    processed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    last_error_code = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true),
                    sanitized_last_error = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_integration_outbox_messages", x => x.id);
                    table.CheckConstraint("ck_integration_outbox_attempts", "attempt_count >= 0");
                    table.CheckConstraint("ck_integration_outbox_schema", "payload_schema_version > 0");
                    table.CheckConstraint("ck_integration_outbox_sequence", "aggregate_sequence > 0");
                    table.CheckConstraint("ck_integration_outbox_status", "status IN ('PENDING','PROCESSING','DELIVERED','FAILED_RETRYABLE','FAILED_FINAL')");
                    table.ForeignKey(
                        name: "fk_integration_outbox_tenant",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "platform_tenant_onboarding_drafts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    owner_platform_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "varchar(24)", maxLength: 24, nullable: false),
                    current_step = table.Column<short>(type: "smallint", nullable: false),
                    completed_steps_mask = table.Column<short>(type: "smallint", nullable: false),
                    progress_percent = table.Column<short>(type: "smallint", nullable: false),
                    payload_json = table.Column<string>(type: "jsonb", nullable: false),
                    schema_version = table.Column<int>(type: "integer", nullable: false),
                    tenant_code_normalized = table.Column<string>(type: "varchar(60)", maxLength: 60, nullable: true),
                    tenant_slug_normalized = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true),
                    requested_domain_normalized = table.Column<string>(type: "varchar(253)", maxLength: 253, nullable: true),
                    admin_email_normalized = table.Column<string>(type: "varchar(320)", maxLength: 320, nullable: true),
                    version = table.Column<long>(type: "bigint", nullable: false),
                    finalize_idempotency_key_hash = table.Column<string>(type: "char(64)", maxLength: 64, nullable: true),
                    finalize_request_hash = table.Column<string>(type: "char(64)", maxLength: 64, nullable: true),
                    created_tenant_id = table.Column<Guid>(type: "uuid", nullable: true),
                    last_error_code = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    discarded_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    finalized_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_by_platform_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    updated_by_platform_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_platform_tenant_onboarding_drafts", x => x.id);
                    table.CheckConstraint("ck_platform_tenant_onboarding_drafts_completed_mask", "completed_steps_mask BETWEEN 0 AND 127");
                    table.CheckConstraint("ck_platform_tenant_onboarding_drafts_current_step", "current_step BETWEEN 1 AND 7");
                    table.CheckConstraint("ck_platform_tenant_onboarding_drafts_progress", "progress_percent BETWEEN 0 AND 100");
                    table.CheckConstraint("ck_platform_tenant_onboarding_drafts_schema", "schema_version > 0");
                    table.CheckConstraint("ck_platform_tenant_onboarding_drafts_status", "status IN ('in_progress','finalizing','completed','discarded','expired')");
                    table.CheckConstraint("ck_platform_tenant_onboarding_drafts_version", "version > 0");
                    table.ForeignKey(
                        name: "fk_onboarding_drafts_created_by_platform_users",
                        column: x => x.created_by_platform_user_id,
                        principalTable: "platform_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_onboarding_drafts_created_tenant",
                        column: x => x.created_tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_onboarding_drafts_owner_platform_users",
                        column: x => x.owner_platform_user_id,
                        principalTable: "platform_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_onboarding_drafts_updated_by_platform_users",
                        column: x => x.updated_by_platform_user_id,
                        principalTable: "platform_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "tenant_contacts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    contact_type = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    contact_name = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false),
                    email = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true),
                    phone = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: true),
                    status = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    created_by_platform_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    updated_by_platform_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tenant_contacts", x => x.id);
                    table.CheckConstraint("ck_tenant_contacts_billing_email", "contact_type <> 'BILLING' OR email IS NOT NULL");
                    table.CheckConstraint("ck_tenant_contacts_reachable", "contact_type <> 'SUPPORT' OR email IS NOT NULL OR phone IS NOT NULL");
                    table.CheckConstraint("ck_tenant_contacts_status", "status IN ('ACTIVE','INACTIVE')");
                    table.CheckConstraint("ck_tenant_contacts_type", "contact_type IN ('BILLING','SUPPORT')");
                    table.ForeignKey(
                        name: "fk_tenant_contacts_created_by",
                        column: x => x.created_by_platform_user_id,
                        principalTable: "platform_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_tenant_contacts_tenant",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_tenant_contacts_updated_by",
                        column: x => x.updated_by_platform_user_id,
                        principalTable: "platform_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "platform_tenant_onboarding_operations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    draft_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    operation_type = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: false),
                    status = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false),
                    provisioning_status = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false),
                    payment_status = table.Column<string>(type: "varchar(24)", maxLength: 24, nullable: false),
                    invitation_status = table.Column<string>(type: "varchar(24)", maxLength: 24, nullable: false),
                    idempotency_key_hash = table.Column<string>(type: "char(64)", maxLength: 64, nullable: false),
                    request_hash = table.Column<string>(type: "char(64)", maxLength: 64, nullable: false),
                    attempt_count = table.Column<int>(type: "integer", nullable: false),
                    started_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    completed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    next_retry_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    failure_code = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true),
                    sanitized_failure_details = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true),
                    result_reference = table.Column<string>(type: "varchar(160)", maxLength: 160, nullable: true),
                    version = table.Column<long>(type: "bigint", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_platform_tenant_onboarding_operations", x => x.id);
                    table.CheckConstraint("ck_onboarding_operations_attempts", "attempt_count >= 0");
                    table.CheckConstraint("ck_onboarding_operations_invitation_status", "invitation_status IN ('NOT_ELIGIBLE','PENDING','SENT','FAILED','ACCEPTED','EXPIRED')");
                    table.CheckConstraint("ck_onboarding_operations_payment_status", "payment_status IN ('NOT_REQUIRED','PENDING','CONFIRMED','FAILED','WAIVED')");
                    table.CheckConstraint("ck_onboarding_operations_provisioning_status", "provisioning_status IN ('PROCESSING','SUCCEEDED','FAILED_RETRYABLE','FAILED_FINAL')");
                    table.CheckConstraint("ck_onboarding_operations_status", "status IN ('PROCESSING','SUCCEEDED','FAILED_RETRYABLE','FAILED_FINAL')");
                    table.ForeignKey(
                        name: "fk_onboarding_operations_draft",
                        column: x => x.draft_id,
                        principalTable: "platform_tenant_onboarding_drafts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_onboarding_operations_tenant",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_tenant_profiles_registration_number",
                table: "tenant_profiles",
                column: "registration_number");

            migrationBuilder.CreateIndex(
                name: "ix_tenant_profiles_tax_number",
                table: "tenant_profiles",
                column: "tax_number");

            migrationBuilder.AddCheckConstraint(
                name: "ck_tenant_feature_entitlements_override_reason",
                table: "tenant_feature_entitlements",
                sql: "source_type <> 'OVERRIDE' OR length(btrim(override_reason)) > 0");

            migrationBuilder.Sql("UPDATE tenant_feature_entitlements SET source_type = 'MANUAL' WHERE source_type NOT IN ('MANUAL', 'PLAN', 'ADDON', 'OVERRIDE');");

            migrationBuilder.AddCheckConstraint(
                name: "ck_tenant_feature_entitlements_source_type",
                table: "tenant_feature_entitlements",
                sql: "source_type IN ('MANUAL', 'PLAN', 'ADDON', 'OVERRIDE')");

            migrationBuilder.CreateIndex(
                name: "uq_subscription_payment_transactions_idempotency_key",
                table: "subscription_payment_transactions",
                column: "idempotency_key",
                unique: true,
                filter: "idempotency_key IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_integration_outbox_claim",
                table: "integration_outbox_messages",
                columns: new[] { "status", "available_at" });

            migrationBuilder.CreateIndex(
                name: "IX_integration_outbox_messages_tenant_id",
                table: "integration_outbox_messages",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "uq_integration_outbox_aggregate_sequence",
                table: "integration_outbox_messages",
                columns: new[] { "aggregate_type", "aggregate_id", "aggregate_sequence" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_integration_outbox_deduplication_key",
                table: "integration_outbox_messages",
                column: "deduplication_key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_onboarding_drafts_owner_status_updated",
                table: "platform_tenant_onboarding_drafts",
                columns: new[] { "owner_platform_user_id", "status", "updated_at" });

            migrationBuilder.CreateIndex(
                name: "ix_onboarding_drafts_requested_domain",
                table: "platform_tenant_onboarding_drafts",
                column: "requested_domain_normalized");

            migrationBuilder.CreateIndex(
                name: "ix_onboarding_drafts_tenant_code",
                table: "platform_tenant_onboarding_drafts",
                column: "tenant_code_normalized");

            migrationBuilder.CreateIndex(
                name: "ix_onboarding_drafts_tenant_slug",
                table: "platform_tenant_onboarding_drafts",
                column: "tenant_slug_normalized");

            migrationBuilder.CreateIndex(
                name: "IX_platform_tenant_onboarding_drafts_created_by_platform_user_~",
                table: "platform_tenant_onboarding_drafts",
                column: "created_by_platform_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_platform_tenant_onboarding_drafts_created_tenant_id",
                table: "platform_tenant_onboarding_drafts",
                column: "created_tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_platform_tenant_onboarding_drafts_updated_by_platform_user_~",
                table: "platform_tenant_onboarding_drafts",
                column: "updated_by_platform_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_onboarding_operations_retry",
                table: "platform_tenant_onboarding_operations",
                columns: new[] { "status", "next_retry_at" });

            migrationBuilder.CreateIndex(
                name: "uq_onboarding_operations_draft",
                table: "platform_tenant_onboarding_operations",
                column: "draft_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_onboarding_operations_tenant",
                table: "platform_tenant_onboarding_operations",
                column: "tenant_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tenant_contacts_created_by_platform_user_id",
                table: "tenant_contacts",
                column: "created_by_platform_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_tenant_contacts_updated_by_platform_user_id",
                table: "tenant_contacts",
                column: "updated_by_platform_user_id");

            migrationBuilder.CreateIndex(
                name: "uq_tenant_contacts_active_type",
                table: "tenant_contacts",
                columns: new[] { "tenant_id", "contact_type" },
                unique: true,
                filter: "status = 'ACTIVE'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "integration_outbox_messages");

            migrationBuilder.DropTable(
                name: "platform_tenant_onboarding_operations");

            migrationBuilder.DropTable(
                name: "tenant_contacts");

            migrationBuilder.DropTable(
                name: "platform_tenant_onboarding_drafts");

            migrationBuilder.DropIndex(
                name: "ix_tenant_profiles_registration_number",
                table: "tenant_profiles");

            migrationBuilder.DropIndex(
                name: "ix_tenant_profiles_tax_number",
                table: "tenant_profiles");

            migrationBuilder.DropCheckConstraint(
                name: "ck_tenant_feature_entitlements_override_reason",
                table: "tenant_feature_entitlements");

            migrationBuilder.DropCheckConstraint(
                name: "ck_tenant_feature_entitlements_source_type",
                table: "tenant_feature_entitlements");

            migrationBuilder.DropIndex(
                name: "uq_subscription_payment_transactions_idempotency_key",
                table: "subscription_payment_transactions");

            migrationBuilder.DropColumn(
                name: "registration_number",
                table: "tenant_profiles");

            migrationBuilder.DropColumn(
                name: "tax_number",
                table: "tenant_profiles");

            migrationBuilder.DropColumn(
                name: "override_reason",
                table: "tenant_feature_entitlements");

            migrationBuilder.AlterColumn<string>(
                name: "billing_cycle",
                table: "tenant_subscriptions",
                type: "varchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "monthly",
                oldClrType: typeof(string),
                oldType: "varchar(20)",
                oldMaxLength: 20);
        }
    }
}
