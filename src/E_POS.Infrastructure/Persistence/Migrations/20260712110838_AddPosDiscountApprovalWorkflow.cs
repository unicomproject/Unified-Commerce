using System;
using Microsoft.EntityFrameworkCore.Migrations;
using E_POS.Infrastructure.Persistence.Seed;

#nullable disable

namespace E_POS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPosDiscountApprovalWorkflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "pos_discount_applications",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    discount_policy_id = table.Column<Guid>(type: "uuid", nullable: false),
                    discount_type_id = table.Column<Guid>(type: "uuid", nullable: false),
                    outlet_id = table.Column<Guid>(type: "uuid", nullable: false),
                    till_id = table.Column<Guid>(type: "uuid", nullable: false),
                    till_session_id = table.Column<Guid>(type: "uuid", nullable: false),
                    pos_device_id = table.Column<Guid>(type: "uuid", nullable: false),
                    requested_by_tenant_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    customer_id = table.Column<Guid>(type: "uuid", nullable: true),
                    target_product_variant_id = table.Column<Guid>(type: "uuid", nullable: true),
                    idempotency_key = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    discount_source = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    discount_scope = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    policy_code_snapshot = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: false),
                    policy_name_snapshot = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: false),
                    calculation_method_snapshot = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: false),
                    requested_value = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    cashier_limit_snapshot = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    absolute_limit_snapshot = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    cart_subtotal_snapshot = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    eligible_subtotal_snapshot = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    discount_amount_snapshot = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    total_after_discount_snapshot = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    currency_code = table.Column<string>(type: "char(3)", maxLength: 3, nullable: false),
                    cart_snapshot_json = table.Column<string>(type: "jsonb", nullable: false),
                    cart_hash = table.Column<string>(type: "char(64)", maxLength: 64, nullable: false),
                    request_reason = table.Column<string>(type: "text", nullable: true),
                    application_status = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false),
                    requires_manager_approval = table.Column<bool>(type: "boolean", nullable: false),
                    requested_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    decided_by_tenant_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    decided_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    decision_note = table.Column<string>(type: "text", nullable: true),
                    sales_order_id = table.Column<Guid>(type: "uuid", nullable: true),
                    applied_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_pos_discount_applications", x => x.id);
                    table.UniqueConstraint("AK_pos_discount_applications_tenant_id_id", x => new { x.tenant_id, x.id });
                    table.CheckConstraint("ck_pos_discount_applications_amounts", "cart_subtotal_snapshot >= 0 AND eligible_subtotal_snapshot >= 0 AND discount_amount_snapshot >= 0 AND total_after_discount_snapshot >= 0");
                    table.CheckConstraint("ck_pos_discount_applications_scope", "discount_scope IN ('ORDER', 'LINE')");
                    table.CheckConstraint("ck_pos_discount_applications_source", "discount_source IN ('POLICY', 'MANUAL')");
                    table.CheckConstraint("ck_pos_discount_applications_status", "application_status IN ('PENDING_APPROVAL', 'APPROVED', 'REJECTED', 'EXPIRED', 'APPLIED', 'CANCELLED')");
                    table.CheckConstraint("ck_pos_discount_applications_values", "requested_value > 0 AND cashier_limit_snapshot >= 0 AND absolute_limit_snapshot > 0");
                    table.ForeignKey(
                        name: "FK_pos_discount_applications_currencies_currency_code",
                        column: x => x.currency_code,
                        principalTable: "currencies",
                        principalColumn: "currency_code",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_pos_discount_applications_customers_tenant_id_customer_id",
                        columns: x => new { x.tenant_id, x.customer_id },
                        principalTable: "customers",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_pos_discount_applications_discount_policies_tenant_id_disco~",
                        columns: x => new { x.tenant_id, x.discount_policy_id },
                        principalTable: "discount_policies",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_pos_discount_applications_discount_types_discount_type_id",
                        column: x => x.discount_type_id,
                        principalTable: "discount_types",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_pos_discount_applications_outlets_tenant_id_outlet_id",
                        columns: x => new { x.tenant_id, x.outlet_id },
                        principalTable: "outlets",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_pos_discount_applications_pos_devices_tenant_id_pos_device_~",
                        columns: x => new { x.tenant_id, x.pos_device_id },
                        principalTable: "pos_devices",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_pos_discount_applications_product_variants_tenant_id_target~",
                        columns: x => new { x.tenant_id, x.target_product_variant_id },
                        principalTable: "product_variants",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_pos_discount_applications_sales_orders_tenant_id_sales_orde~",
                        columns: x => new { x.tenant_id, x.sales_order_id },
                        principalTable: "sales_orders",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_pos_discount_applications_tenant_users_decided_by_tenant_us~",
                        column: x => x.decided_by_tenant_user_id,
                        principalTable: "tenant_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_pos_discount_applications_tenant_users_requested_by_tenant_~",
                        column: x => x.requested_by_tenant_user_id,
                        principalTable: "tenant_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_pos_discount_applications_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_pos_discount_applications_till_sessions_tenant_id_till_sess~",
                        columns: x => new { x.tenant_id, x.till_session_id },
                        principalTable: "till_sessions",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_pos_discount_applications_tills_tenant_id_till_id",
                        columns: x => new { x.tenant_id, x.till_id },
                        principalTable: "tills",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "pos_discount_authority_limits",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    max_percentage = table.Column<decimal>(type: "numeric(8,4)", precision: 8, scale: 4, nullable: false),
                    max_fixed_amount = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    currency_code = table.Column<string>(type: "char(3)", maxLength: 3, nullable: false),
                    status = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false),
                    created_by_tenant_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by_tenant_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_pos_discount_authority_limits", x => x.id);
                    table.CheckConstraint("ck_pos_discount_authority_limits_fixed", "max_fixed_amount >= 0");
                    table.CheckConstraint("ck_pos_discount_authority_limits_percentage", "max_percentage >= 0 AND max_percentage <= 100");
                    table.CheckConstraint("ck_pos_discount_authority_limits_status", "status IN ('ACTIVE', 'INACTIVE', 'DELETED')");
                    table.ForeignKey(
                        name: "FK_pos_discount_authority_limits_currencies_currency_code",
                        column: x => x.currency_code,
                        principalTable: "currencies",
                        principalColumn: "currency_code",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_pos_discount_authority_limits_tenant_users_created_by_tenan~",
                        column: x => x.created_by_tenant_user_id,
                        principalTable: "tenant_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_pos_discount_authority_limits_tenant_users_tenant_user_id",
                        column: x => x.tenant_user_id,
                        principalTable: "tenant_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_pos_discount_authority_limits_tenant_users_updated_by_tenan~",
                        column: x => x.updated_by_tenant_user_id,
                        principalTable: "tenant_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_pos_discount_authority_limits_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "pos_discount_application_events",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    pos_discount_application_id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_type = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: false),
                    from_status = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false),
                    to_status = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false),
                    actor_tenant_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    note = table.Column<string>(type: "text", nullable: true),
                    occurred_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_pos_discount_application_events", x => x.id);
                    table.ForeignKey(
                        name: "FK_pos_discount_application_events_pos_discount_applications_t~",
                        columns: x => new { x.tenant_id, x.pos_discount_application_id },
                        principalTable: "pos_discount_applications",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_pos_discount_application_events_tenant_users_actor_tenant_u~",
                        column: x => x.actor_tenant_user_id,
                        principalTable: "tenant_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_pos_discount_application_events_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_pos_discount_application_events_actor_tenant_user_id",
                table: "pos_discount_application_events",
                column: "actor_tenant_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_pos_discount_application_events_application",
                table: "pos_discount_application_events",
                columns: new[] { "tenant_id", "pos_discount_application_id", "occurred_at" });

            migrationBuilder.CreateIndex(
                name: "IX_pos_discount_applications_currency_code",
                table: "pos_discount_applications",
                column: "currency_code");

            migrationBuilder.CreateIndex(
                name: "IX_pos_discount_applications_decided_by_tenant_user_id",
                table: "pos_discount_applications",
                column: "decided_by_tenant_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_pos_discount_applications_discount_type_id",
                table: "pos_discount_applications",
                column: "discount_type_id");

            migrationBuilder.CreateIndex(
                name: "IX_pos_discount_applications_requested_by_tenant_user_id",
                table: "pos_discount_applications",
                column: "requested_by_tenant_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_pos_discount_applications_status_expiry",
                table: "pos_discount_applications",
                columns: new[] { "tenant_id", "application_status", "expires_at" });

            migrationBuilder.CreateIndex(
                name: "IX_pos_discount_applications_tenant_id_customer_id",
                table: "pos_discount_applications",
                columns: new[] { "tenant_id", "customer_id" });

            migrationBuilder.CreateIndex(
                name: "IX_pos_discount_applications_tenant_id_discount_policy_id",
                table: "pos_discount_applications",
                columns: new[] { "tenant_id", "discount_policy_id" });

            migrationBuilder.CreateIndex(
                name: "IX_pos_discount_applications_tenant_id_outlet_id",
                table: "pos_discount_applications",
                columns: new[] { "tenant_id", "outlet_id" });

            migrationBuilder.CreateIndex(
                name: "IX_pos_discount_applications_tenant_id_pos_device_id",
                table: "pos_discount_applications",
                columns: new[] { "tenant_id", "pos_device_id" });

            migrationBuilder.CreateIndex(
                name: "IX_pos_discount_applications_tenant_id_sales_order_id",
                table: "pos_discount_applications",
                columns: new[] { "tenant_id", "sales_order_id" });

            migrationBuilder.CreateIndex(
                name: "IX_pos_discount_applications_tenant_id_target_product_variant_~",
                table: "pos_discount_applications",
                columns: new[] { "tenant_id", "target_product_variant_id" });

            migrationBuilder.CreateIndex(
                name: "IX_pos_discount_applications_tenant_id_till_id",
                table: "pos_discount_applications",
                columns: new[] { "tenant_id", "till_id" });

            migrationBuilder.CreateIndex(
                name: "IX_pos_discount_applications_tenant_id_till_session_id",
                table: "pos_discount_applications",
                columns: new[] { "tenant_id", "till_session_id" });

            migrationBuilder.CreateIndex(
                name: "uq_pos_discount_applications_idempotency",
                table: "pos_discount_applications",
                columns: new[] { "tenant_id", "requested_by_tenant_user_id", "idempotency_key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_pos_discount_authority_limits_created_by_tenant_user_id",
                table: "pos_discount_authority_limits",
                column: "created_by_tenant_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_pos_discount_authority_limits_currency_code",
                table: "pos_discount_authority_limits",
                column: "currency_code");

            migrationBuilder.CreateIndex(
                name: "IX_pos_discount_authority_limits_tenant_user_id",
                table: "pos_discount_authority_limits",
                column: "tenant_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_pos_discount_authority_limits_updated_by_tenant_user_id",
                table: "pos_discount_authority_limits",
                column: "updated_by_tenant_user_id");

            migrationBuilder.CreateIndex(
                name: "uq_pos_discount_authority_limits_tenant_user",
                table: "pos_discount_authority_limits",
                columns: new[] { "tenant_id", "tenant_user_id" },
                unique: true);

            migrationBuilder.Sql(DevelopmentPosDiscountWorkflowSeedData.UpSql);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(DevelopmentPosDiscountWorkflowSeedData.DownSql);

            migrationBuilder.DropTable(
                name: "pos_discount_application_events");

            migrationBuilder.DropTable(
                name: "pos_discount_authority_limits");

            migrationBuilder.DropTable(
                name: "pos_discount_applications");
        }
    }
}
