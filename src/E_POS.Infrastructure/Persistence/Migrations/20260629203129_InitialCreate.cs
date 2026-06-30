using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E_POS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:citext", ",,");

            migrationBuilder.CreateTable(
                name: "business_types",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    status = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false),
                    business_type_code = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_business_types", x => x.id);
                    table.CheckConstraint("ck_business_types_status", "status IN ('ACTIVE', 'INACTIVE', 'DELETED')");
                });

            migrationBuilder.CreateTable(
                name: "cash_movement_types",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false),
                    status = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false),
                    movement_type_code = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_cash_movement_types", x => x.id);
                    table.CheckConstraint("ck_cash_movement_types_status", "status IN ('ACTIVE', 'INACTIVE', 'DELETED')");
                });

            migrationBuilder.CreateTable(
                name: "currencies",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    currency_code = table.Column<string>(type: "char(3)", maxLength: 3, nullable: false),
                    name = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false),
                    decimal_places = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_currencies", x => x.id);
                    table.CheckConstraint("ck_currencies_decimal_places", "decimal_places >= 0");
                });

            migrationBuilder.CreateTable(
                name: "document_number_sequences",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    outlet_id = table.Column<Guid>(type: "uuid", nullable: false),
                    document_type = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: false),
                    document_subtype = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: false),
                    current_value = table.Column<int>(type: "integer", nullable: false),
                    padding_length = table.Column<int>(type: "integer", nullable: false),
                    sales_channel_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_document_number_sequences", x => x.id);
                    table.CheckConstraint("ck_document_number_sequences_current_value", "current_value >= 0");
                    table.CheckConstraint("ck_document_number_sequences_padding_length", "padding_length > 0");
                });

            migrationBuilder.CreateTable(
                name: "fulfillment_methods",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    method_code = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: false),
                    name = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    status = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false),
                    method_type = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_fulfillment_methods", x => x.id);
                    table.CheckConstraint("ck_fulfillment_methods_method_type", "method_type IN ('IMMEDIATE', 'PICKUP')");
                });

            migrationBuilder.CreateTable(
                name: "integration_providers",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    provider_code = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: false),
                    name = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    provider_category = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_integration_providers", x => x.id);
                    table.CheckConstraint("ck_integration_providers_provider_category", "provider_category IN ('PAYMENT', 'SMS', 'EMAIL', 'WHATSAPP', 'ACCOUNTING', 'DELIVERY', 'ANALYTICS', 'OTHER')");
                });

            migrationBuilder.CreateTable(
                name: "notification_event_types",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    default_priority = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false),
                    event_code = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_notification_event_types", x => x.id);
                    table.CheckConstraint("ck_notification_event_types_default_priority", "default_priority IN ('LOW', 'NORMAL', 'HIGH', 'URGENT')");
                });

            migrationBuilder.CreateTable(
                name: "payment_methods",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    method_code = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: false),
                    name = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    status = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false),
                    method_type = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_payment_methods", x => x.id);
                    table.CheckConstraint("ck_payment_methods_method_type", "method_type IN ('CASH', 'CARD', 'QR', 'BANK_TRANSFER', 'MANUAL', 'OTHER')");
                });

            migrationBuilder.CreateTable(
                name: "permission_definitions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    permission_code = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: false),
                    name = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false),
                    status = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_permission_definitions", x => x.id);
                    table.CheckConstraint("ck_permission_definitions_status", "status IN ('ACTIVE', 'INACTIVE', 'DELETED')");
                });

            migrationBuilder.CreateTable(
                name: "platform_modules",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    module_code = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: false),
                    name = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    status = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_platform_modules", x => x.id);
                    table.CheckConstraint("ck_platform_modules_status", "status IN ('ACTIVE', 'INACTIVE', 'DELETED')");
                });

            migrationBuilder.CreateTable(
                name: "platform_permissions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    permission_code = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: false),
                    name = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    status = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_platform_permissions", x => x.id);
                    table.CheckConstraint("ck_platform_permissions_status", "status IN ('ACTIVE', 'INACTIVE', 'DELETED')");
                });

            migrationBuilder.CreateTable(
                name: "platform_roles",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    role_code = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: false),
                    name = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    status = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_platform_roles", x => x.id);
                    table.CheckConstraint("ck_platform_roles_status", "status IN ('ACTIVE', 'INACTIVE', 'DELETED')");
                });

            migrationBuilder.CreateTable(
                name: "platform_users",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    email = table.Column<string>(type: "citext", nullable: false),
                    normalized_email = table.Column<string>(type: "citext", nullable: false),
                    status = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_platform_users", x => x.id);
                    table.CheckConstraint("ck_platform_users_status", "status IN ('ACTIVE', 'INACTIVE', 'LOCKED', 'DELETED')");
                });

            migrationBuilder.CreateTable(
                name: "receipt_templates",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    template_code = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: false),
                    name = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    status = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false),
                    parent_template_id = table.Column<Guid>(type: "uuid", nullable: true),
                    sort_order = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_receipt_templates", x => x.id);
                    table.ForeignKey(
                        name: "fk_receipt_templates_parent_template_id_receipt_templates",
                        column: x => x.parent_template_id,
                        principalTable: "receipt_templates",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "return_reasons",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    reason_code = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: false),
                    name = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    applies_to = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_return_reasons", x => x.id);
                    table.CheckConstraint("ck_return_reasons_applies_to", "applies_to IN ('RETURN', 'EXCHANGE', 'BOTH')");
                });

            migrationBuilder.CreateTable(
                name: "role_templates",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    template_code = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: false),
                    name = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    status = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_role_templates", x => x.id);
                    table.CheckConstraint("ck_role_templates_status", "status IN ('ACTIVE', 'INACTIVE', 'DELETED')");
                });

            migrationBuilder.CreateTable(
                name: "setting_definitions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    setting_key = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false),
                    value_type = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_setting_definitions", x => x.id);
                    table.CheckConstraint("ck_setting_definitions_value_type", "value_type IN ('STRING', 'NUMBER', 'BOOLEAN', 'JSON', 'DATE')");
                });

            migrationBuilder.CreateTable(
                name: "subscription_addons",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    addon_code = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: false),
                    name = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    status = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false),
                    price_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_subscription_addons", x => x.id);
                    table.CheckConstraint("ck_subscription_addons_price_amount", "price_amount >= 0");
                    table.CheckConstraint("ck_subscription_addons_status", "status IN ('ACTIVE', 'INACTIVE', 'DELETED')");
                });

            migrationBuilder.CreateTable(
                name: "subscription_plans",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    plan_code = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: false),
                    name = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    status = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false),
                    billing_interval = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false),
                    price_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_subscription_plans", x => x.id);
                    table.CheckConstraint("ck_subscription_plans_billing_interval", "billing_interval IN ('MONTHLY', 'YEARLY', 'ONE_TIME')");
                    table.CheckConstraint("ck_subscription_plans_price_amount", "price_amount >= 0");
                });

            migrationBuilder.CreateTable(
                name: "tenants",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_code = table.Column<string>(type: "varchar(60)", maxLength: 60, nullable: false),
                    currency_code = table.Column<string>(type: "char(3)", maxLength: 3, nullable: false),
                    name = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false),
                    status = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false),
                    base_currency = table.Column<string>(type: "char(3)", maxLength: 3, nullable: false),
                    billing_status = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false),
                    business_type = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: true),
                    business_type_id = table.Column<Guid>(type: "uuid", nullable: false),
                    default_locale = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    default_timezone = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    operating_mode = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false),
                    primary_domain = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tenants", x => x.id);
                    table.ForeignKey(
                        name: "fk_tenants_business_type_id_business_types",
                        column: x => x.business_type_id,
                        principalTable: "business_types",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "platform_integrations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    integration_code = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: false),
                    currency_code = table.Column<string>(type: "char(3)", maxLength: 3, nullable: false),
                    name = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    status = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false),
                    integration_provider_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_platform_integrations", x => x.id);
                    table.ForeignKey(
                        name: "fk_platform_integrations_integration_provider_id_integration_providers",
                        column: x => x.integration_provider_id,
                        principalTable: "integration_providers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "notification_events",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_number = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: false),
                    idempotency_key = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: false),
                    notification_event_type_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_notification_events", x => x.id);
                    table.ForeignKey(
                        name: "fk_notification_events_notification_event_type_id_notification_event_types",
                        column: x => x.notification_event_type_id,
                        principalTable: "notification_event_types",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "notification_preferences",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    platform_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    customer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    channel_type = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: false),
                    recipient_type = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: false),
                    notification_event_type_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_notification_preferences", x => x.id);
                    table.CheckConstraint("ck_notification_preferences_recipient_type_platform_user_id_te~", "(recipient_type = 'PLATFORM_USER' AND platform_user_id IS NOT NULL AND tenant_user_id IS NULL AND customer_id IS NULL) OR (recipient_type = 'TENANT_USER' AND tenant_user_id IS NOT NULL AND platform_user_id IS NULL AND customer_id IS NULL) OR (recipient_type = 'CUSTOMER' AND customer_id IS NOT NULL AND platform_user_id IS NULL AND tenant_user_id IS NULL)");
                    table.ForeignKey(
                        name: "fk_notification_preferences_notification_event_type_id_notification_event_types",
                        column: x => x.notification_event_type_id,
                        principalTable: "notification_event_types",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "notification_templates",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    template_code = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: false),
                    name = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    status = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false),
                    channel_type = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: false),
                    locale = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    notification_event_type_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_notification_templates", x => x.id);
                    table.ForeignKey(
                        name: "fk_notification_templates_notification_event_type_id_notification_event_types",
                        column: x => x.notification_event_type_id,
                        principalTable: "notification_event_types",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "platform_features",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    feature_code = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: false),
                    name = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    status = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false),
                    platform_module_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_platform_features", x => x.id);
                    table.CheckConstraint("ck_platform_features_status", "status IN ('ACTIVE', 'INACTIVE', 'DELETED')");
                    table.ForeignKey(
                        name: "fk_platform_features_platform_module_id_platform_modules",
                        column: x => x.platform_module_id,
                        principalTable: "platform_modules",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "platform_role_permissions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    platform_permission_id = table.Column<Guid>(type: "uuid", nullable: false),
                    platform_role_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_platform_role_permissions", x => x.id);
                    table.ForeignKey(
                        name: "fk_platform_role_permissions_platform_permission_id_platform_permissions",
                        column: x => x.platform_permission_id,
                        principalTable: "platform_permissions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_platform_role_permissions_platform_role_id_platform_roles",
                        column: x => x.platform_role_id,
                        principalTable: "platform_roles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "platform_auth_sessions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    platform_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    status = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false),
                    session_token_hash = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_platform_auth_sessions", x => x.id);
                    table.CheckConstraint("ck_platform_auth_sessions_status", "status IN ('ACTIVE', 'EXPIRED', 'REVOKED')");
                    table.ForeignKey(
                        name: "fk_platform_auth_sessions_platform_user_id_platform_users",
                        column: x => x.platform_user_id,
                        principalTable: "platform_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "platform_login_audits",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    platform_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    login_result = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_platform_login_audits", x => x.id);
                    table.CheckConstraint("ck_platform_login_audits_login_result", "login_result IN ('SUCCESS', 'FAILED', 'LOCKED')");
                    table.ForeignKey(
                        name: "fk_platform_login_audits_platform_user_id_platform_users",
                        column: x => x.platform_user_id,
                        principalTable: "platform_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "platform_password_reset_tokens",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    platform_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    status = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false),
                    token_hash = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_platform_password_reset_tokens", x => x.id);
                    table.CheckConstraint("ck_platform_password_reset_tokens_status", "status IN ('PENDING', 'USED', 'EXPIRED', 'REVOKED')");
                    table.ForeignKey(
                        name: "fk_platform_password_reset_tokens_platform_user_id_platform_users",
                        column: x => x.platform_user_id,
                        principalTable: "platform_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "platform_user_permissions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    platform_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    description = table.Column<string>(type: "text", nullable: true),
                    platform_permission_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_platform_user_permissions", x => x.id);
                    table.ForeignKey(
                        name: "fk_platform_user_permissions_platform_permission_id_platform_permissions",
                        column: x => x.platform_permission_id,
                        principalTable: "platform_permissions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_platform_user_permissions_platform_user_id_platform_users",
                        column: x => x.platform_user_id,
                        principalTable: "platform_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "platform_user_roles",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    platform_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    description = table.Column<string>(type: "text", nullable: true),
                    platform_role_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_platform_user_roles", x => x.id);
                    table.ForeignKey(
                        name: "fk_platform_user_roles_platform_role_id_platform_roles",
                        column: x => x.platform_role_id,
                        principalTable: "platform_roles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_platform_user_roles_platform_user_id_platform_users",
                        column: x => x.platform_user_id,
                        principalTable: "platform_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "receipt_template_versions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    receipt_template_id = table.Column<Guid>(type: "uuid", nullable: false),
                    version_number = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_receipt_template_versions", x => x.id);
                    table.CheckConstraint("ck_receipt_template_versions_version_number", "version_number > 0");
                    table.ForeignKey(
                        name: "fk_receipt_template_versions_receipt_template_id_receipt_templates",
                        column: x => x.receipt_template_id,
                        principalTable: "receipt_templates",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "role_template_versions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    role_template_id = table.Column<Guid>(type: "uuid", nullable: false),
                    version_number = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_role_template_versions", x => x.id);
                    table.CheckConstraint("ck_role_template_versions_version_number", "version_number > 0");
                    table.ForeignKey(
                        name: "fk_role_template_versions_role_template_id_role_templates",
                        column: x => x.role_template_id,
                        principalTable: "role_templates",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "subscription_plan_addons",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    status = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false),
                    subscription_addon_id = table.Column<Guid>(type: "uuid", nullable: false),
                    subscription_plan_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_subscription_plan_addons", x => x.id);
                    table.ForeignKey(
                        name: "fk_subscription_plan_addons_subscription_addon_id_subscription_addons",
                        column: x => x.subscription_addon_id,
                        principalTable: "subscription_addons",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_subscription_plan_addons_subscription_plan_id_subscription_plans",
                        column: x => x.subscription_plan_id,
                        principalTable: "subscription_plans",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "brands",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false),
                    status = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false),
                    brand_code = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_brands", x => x.id);
                    table.CheckConstraint("ck_brands_status", "status IN ('ACTIVE', 'INACTIVE', 'DELETED')");
                    table.ForeignKey(
                        name: "fk_brands_tenant_id_tenants",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "categories",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false),
                    status = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false),
                    category_code = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: false),
                    parent_category_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_categories", x => x.id);
                    table.CheckConstraint("ck_categories_status", "status IN ('ACTIVE', 'INACTIVE', 'DELETED')");
                    table.ForeignKey(
                        name: "fk_categories_parent_category_id_categories",
                        column: x => x.parent_category_id,
                        principalTable: "categories",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_categories_tenant_id_tenants",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "choice_groups",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false),
                    choice_group_code = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: false),
                    max_select = table.Column<int>(type: "integer", nullable: false),
                    min_select = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_choice_groups", x => x.id);
                    table.CheckConstraint("ck_choice_groups_max_select_min_select", "max_select >= min_select");
                    table.CheckConstraint("ck_choice_groups_min_select", "min_select >= 0");
                    table.ForeignKey(
                        name: "fk_choice_groups_tenant_id_tenants",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "collections",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false),
                    status = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false),
                    collection_code = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_collections", x => x.id);
                    table.CheckConstraint("ck_collections_status", "status IN ('ACTIVE', 'INACTIVE', 'DELETED')");
                    table.ForeignKey(
                        name: "fk_collections_tenant_id_tenants",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "customers",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false),
                    normalized_email = table.Column<string>(type: "citext", nullable: false),
                    normalized_phone = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: false),
                    status = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false),
                    customer_code = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_customers", x => x.id);
                    table.ForeignKey(
                        name: "fk_customers_tenant_id_tenants",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "departments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false),
                    status = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false),
                    department_code = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_departments", x => x.id);
                    table.CheckConstraint("ck_departments_status", "status IN ('ACTIVE', 'INACTIVE', 'DELETED')");
                    table.ForeignKey(
                        name: "fk_departments_tenant_id_tenants",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "discount_types",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false),
                    discount_type_code = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_discount_types", x => x.id);
                    table.ForeignKey(
                        name: "fk_discount_types_tenant_id_tenants",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "expiry_discount_rules",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false),
                    rule_code = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_expiry_discount_rules", x => x.id);
                    table.ForeignKey(
                        name: "fk_expiry_discount_rules_tenant_id_tenants",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "hardware_profiles",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false),
                    profile_code = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_hardware_profiles", x => x.id);
                    table.ForeignKey(
                        name: "fk_hardware_profiles_tenant_id_tenants",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "inventory_reservations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    reservation_status = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false),
                    reservation_number = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_inventory_reservations", x => x.id);
                    table.CheckConstraint("ck_inventory_reservations_reservation_status", "reservation_status IN ('PENDING', 'CONFIRMED', 'RELEASED', 'EXPIRED', 'CANCELLED')");
                    table.ForeignKey(
                        name: "fk_inventory_reservations_tenant_id_tenants",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "outlets",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false),
                    status = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false),
                    outlet_code = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_outlets", x => x.id);
                    table.CheckConstraint("ck_outlets_status", "status IN ('ACTIVE', 'INACTIVE', 'DELETED')");
                    table.ForeignKey(
                        name: "fk_outlets_tenant_id_tenants",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "price_lists",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false),
                    status = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false),
                    price_list_code = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_price_lists", x => x.id);
                    table.CheckConstraint("ck_price_lists_status", "status IN ('ACTIVE', 'INACTIVE', 'DELETED')");
                    table.ForeignKey(
                        name: "fk_price_lists_tenant_id_tenants",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "product_attribute_definitions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false),
                    attribute_code = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_product_attribute_definitions", x => x.id);
                    table.ForeignKey(
                        name: "fk_product_attribute_definitions_tenant_id_tenants",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "product_option_templates",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    template_code = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: false),
                    name = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    status = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_product_option_templates", x => x.id);
                    table.ForeignKey(
                        name: "fk_product_option_templates_tenant_id_tenants",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "products",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    status = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false),
                    product_code = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_products", x => x.id);
                    table.CheckConstraint("ck_products_status", "status IN ('ACTIVE', 'INACTIVE', 'DELETED')");
                    table.ForeignKey(
                        name: "fk_products_tenant_id_tenants",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "return_policies",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false),
                    policy_code = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: false),
                    return_window_days = table.Column<int>(type: "integer", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_return_policies", x => x.id);
                    table.CheckConstraint("ck_return_policies_return_window_days", "return_window_days IS NULL OR return_window_days >= 0");
                    table.ForeignKey(
                        name: "fk_return_policies_tenant_id_tenants",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "sales_channels",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    channel_code = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: false),
                    name = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false),
                    channel_type = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: false),
                    status = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sales_channels", x => x.id);
                    table.CheckConstraint("ck_sales_channels_channel_type", "channel_type IN ('E_POS', 'E_COMMERCE')");
                    table.CheckConstraint("ck_sales_channels_status", "status IN ('ACTIVE', 'INACTIVE', 'DELETED')");
                    table.ForeignKey(
                        name: "fk_sales_channels_tenant_id_tenants",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "sales_orders",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false),
                    document_number_sequence_id = table.Column<Guid>(type: "uuid", nullable: false),
                    order_number = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: false),
                    paid_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    total_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sales_orders", x => x.id);
                    table.CheckConstraint("ck_sales_orders_paid_amount", "paid_amount >= 0");
                    table.CheckConstraint("ck_sales_orders_total_amount", "total_amount >= 0");
                    table.ForeignKey(
                        name: "fk_sales_orders_document_number_sequence_id_document_number_sequences",
                        column: x => x.document_number_sequence_id,
                        principalTable: "document_number_sequences",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_sales_orders_tenant_id_tenants",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "shopping_carts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    cart_status = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false),
                    cart_number = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_shopping_carts", x => x.id);
                    table.CheckConstraint("ck_shopping_carts_cart_status", "cart_status IN ('ACTIVE', 'CHECKED_OUT', 'ABANDONED', 'EXPIRED', 'CANCELLED')");
                    table.ForeignKey(
                        name: "fk_shopping_carts_tenant_id_tenants",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "stock_adjustment_reasons",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    reason_code = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: false),
                    name = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_stock_adjustment_reasons", x => x.id);
                    table.ForeignKey(
                        name: "fk_stock_adjustment_reasons_tenant_id_tenants",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "stock_adjustments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    adjustment_number = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: false),
                    adjustment_status = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_stock_adjustments", x => x.id);
                    table.CheckConstraint("ck_stock_adjustments_adjustment_status", "adjustment_status IN ('DRAFT', 'APPROVED', 'POSTED', 'CANCELLED')");
                    table.ForeignKey(
                        name: "fk_stock_adjustments_tenant_id_tenants",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "stock_movements",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    movement_number = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: false),
                    movement_quantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_stock_movements", x => x.id);
                    table.CheckConstraint("ck_stock_movements_movement_quantity", "movement_quantity <> 0");
                    table.ForeignKey(
                        name: "fk_stock_movements_tenant_id_tenants",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "tax_classes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false),
                    tax_class_code = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tax_classes", x => x.id);
                    table.ForeignKey(
                        name: "fk_tax_classes_tenant_id_tenants",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "tax_jurisdictions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false),
                    jurisdiction_code = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tax_jurisdictions", x => x.id);
                    table.ForeignKey(
                        name: "fk_tax_jurisdictions_tenant_id_tenants",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "tenant_addresses",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    address_type = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tenant_addresses", x => x.id);
                    table.CheckConstraint("ck_tenant_addresses_address_type", "address_type IN ('BILLING', 'REGISTERED', 'CONTACT')");
                    table.ForeignKey(
                        name: "fk_tenant_addresses_tenant_id_tenants",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "tenant_domains",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    domain_name = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false),
                    domain_status = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tenant_domains", x => x.id);
                    table.CheckConstraint("ck_tenant_domains_domain_status", "domain_status IN ('PENDING', 'VERIFIED', 'FAILED', 'DISABLED')");
                    table.ForeignKey(
                        name: "fk_tenant_domains_tenant_id_tenants",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "tenant_profiles",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tenant_profiles", x => x.id);
                    table.ForeignKey(
                        name: "fk_tenant_profiles_tenant_id_tenants",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "tenant_settings",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    setting_definition_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tenant_settings", x => x.id);
                    table.ForeignKey(
                        name: "fk_tenant_settings_setting_definition_id_setting_definitions",
                        column: x => x.setting_definition_id,
                        principalTable: "setting_definitions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_tenant_settings_tenant_id_tenants",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "tenant_subscriptions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    subscription_number = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: false),
                    subscription_plan_id = table.Column<Guid>(type: "uuid", nullable: false),
                    subscription_status = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tenant_subscriptions", x => x.id);
                    table.CheckConstraint("ck_tenant_subscriptions_subscription_status", "subscription_status IN ('TRIAL', 'ACTIVE', 'PAST_DUE', 'CANCELLED', 'EXPIRED')");
                    table.ForeignKey(
                        name: "fk_tenant_subscriptions_subscription_plan_id_subscription_plans",
                        column: x => x.subscription_plan_id,
                        principalTable: "subscription_plans",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_tenant_subscriptions_tenant_id_tenants",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "tenant_users",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    normalized_email = table.Column<string>(type: "citext", nullable: false),
                    normalized_phone = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: false),
                    status = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tenant_users", x => x.id);
                    table.CheckConstraint("ck_tenant_users_status", "status IN ('ACTIVE', 'INACTIVE', 'INVITED', 'LOCKED', 'DELETED')");
                    table.ForeignKey(
                        name: "fk_tenant_users_tenant_id_tenants",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "unit_of_measures",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false),
                    conversion_factor = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    uom_code = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_unit_of_measures", x => x.id);
                    table.CheckConstraint("ck_unit_of_measures_conversion_factor", "conversion_factor IS NULL OR conversion_factor > 0");
                    table.ForeignKey(
                        name: "fk_unit_of_measures_tenant_id_tenants",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "user_invites",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    invite_status = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false),
                    invite_token_hash = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_invites", x => x.id);
                    table.CheckConstraint("ck_user_invites_invite_status", "invite_status IN ('PENDING', 'ACCEPTED', 'EXPIRED', 'REVOKED')");
                    table.ForeignKey(
                        name: "fk_user_invites_tenant_id_tenants",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "notification_channels",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false),
                    status = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false),
                    channel_type = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: false),
                    channel_code = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: false),
                    platform_integration_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_notification_channels", x => x.id);
                    table.CheckConstraint("ck_notification_channels_channel_type", "channel_type IN ('EMAIL', 'SMS', 'WHATSAPP', 'PUSH', 'IN_APP')");
                    table.ForeignKey(
                        name: "fk_notification_channels_platform_integration_id_platform_integrations",
                        column: x => x.platform_integration_id,
                        principalTable: "platform_integrations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "platform_integration_credentials",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    credential_name = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: false),
                    platform_integration_id = table.Column<Guid>(type: "uuid", nullable: false),
                    revoked_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_platform_integration_credentials", x => x.id);
                    table.CheckConstraint("ck_platform_integration_credentials_revoked_at_created_at", "revoked_at IS NULL OR revoked_at >= created_at");
                    table.ForeignKey(
                        name: "fk_platform_integration_credentials_platform_integration_id_platform_integrations",
                        column: x => x.platform_integration_id,
                        principalTable: "platform_integrations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "platform_integration_request_logs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false),
                    duration_ms = table.Column<int>(type: "integer", nullable: false),
                    integration_provider_id = table.Column<Guid>(type: "uuid", nullable: false),
                    platform_integration_id = table.Column<Guid>(type: "uuid", nullable: false),
                    response_status_code = table.Column<int>(type: "integer", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_platform_integration_request_logs", x => x.id);
                    table.CheckConstraint("ck_platform_integration_request_logs_duration_ms", "duration_ms IS NULL OR duration_ms >= 0");
                    table.CheckConstraint("ck_platform_integration_request_logs_response_status_code", "response_status_code IS NULL OR response_status_code >= 100");
                    table.ForeignKey(
                        name: "fk_platform_integration_request_logs_integration_provider_id_integration_providers",
                        column: x => x.integration_provider_id,
                        principalTable: "integration_providers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_platform_integration_request_logs_platform_integration_id_platform_integrations",
                        column: x => x.platform_integration_id,
                        principalTable: "platform_integrations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "platform_integration_webhook_events",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    external_event_id = table.Column<Guid>(type: "uuid", nullable: false),
                    idempotency_key = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: false),
                    integration_provider_id = table.Column<Guid>(type: "uuid", nullable: false),
                    platform_integration_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_platform_integration_webhook_events", x => x.id);
                    table.ForeignKey(
                        name: "fk_platform_integration_webhook_events_integration_provider_id_integration_providers",
                        column: x => x.integration_provider_id,
                        principalTable: "integration_providers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_platform_integration_webhook_events_platform_integration_id_platform_integrations",
                        column: x => x.platform_integration_id,
                        principalTable: "platform_integrations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "notification_template_versions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    notification_template_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active_version = table.Column<bool>(type: "boolean", nullable: false),
                    version_number = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_notification_template_versions", x => x.id);
                    table.CheckConstraint("ck_notification_template_versions_version_number", "version_number > 0");
                    table.ForeignKey(
                        name: "fk_notification_template_versions_notification_template_id_notification_templates",
                        column: x => x.notification_template_id,
                        principalTable: "notification_templates",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "feature_flags",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false),
                    status = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false),
                    flag_code = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: false),
                    platform_feature_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_feature_flags", x => x.id);
                    table.CheckConstraint("ck_feature_flags_status", "status IN ('ACTIVE', 'INACTIVE', 'DELETED')");
                    table.ForeignKey(
                        name: "fk_feature_flags_platform_feature_id_platform_features",
                        column: x => x.platform_feature_id,
                        principalTable: "platform_features",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "feature_limit_definitions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    limit_code = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: false),
                    name = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false),
                    default_limit_value = table.Column<int>(type: "integer", nullable: true),
                    platform_feature_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_feature_limit_definitions", x => x.id);
                    table.CheckConstraint("ck_feature_limit_definitions_default_limit_value", "default_limit_value IS NULL OR default_limit_value >= 0");
                    table.ForeignKey(
                        name: "fk_feature_limit_definitions_platform_feature_id_platform_features",
                        column: x => x.platform_feature_id,
                        principalTable: "platform_features",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "subscription_addon_features",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    status = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false),
                    platform_feature_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    subscription_addon_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_subscription_addon_features", x => x.id);
                    table.ForeignKey(
                        name: "fk_subscription_addon_features_platform_feature_id_platform_features",
                        column: x => x.platform_feature_id,
                        principalTable: "platform_features",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_subscription_addon_features_subscription_addon_id_subscription_addons",
                        column: x => x.subscription_addon_id,
                        principalTable: "subscription_addons",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "subscription_plan_features",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    status = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false),
                    platform_feature_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    subscription_plan_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_subscription_plan_features", x => x.id);
                    table.ForeignKey(
                        name: "fk_subscription_plan_features_platform_feature_id_platform_features",
                        column: x => x.platform_feature_id,
                        principalTable: "platform_features",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_subscription_plan_features_subscription_plan_id_subscription_plans",
                        column: x => x.subscription_plan_id,
                        principalTable: "subscription_plans",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "tenant_feature_entitlements",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    entitlement_status = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false),
                    platform_feature_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tenant_feature_entitlements", x => x.id);
                    table.CheckConstraint("ck_tenant_feature_entitlements_entitlement_status", "entitlement_status IN ('ENABLED', 'DISABLED', 'EXPIRED')");
                    table.ForeignKey(
                        name: "fk_tenant_feature_entitlements_platform_feature_id_platform_features",
                        column: x => x.platform_feature_id,
                        principalTable: "platform_features",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_tenant_feature_entitlements_tenant_id_tenants",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "tenant_usage_counters",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    platform_feature_id = table.Column<Guid>(type: "uuid", nullable: false),
                    usage_period_start = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false),
                    used_quantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tenant_usage_counters", x => x.id);
                    table.CheckConstraint("ck_tenant_usage_counters_used_quantity", "used_quantity >= 0");
                    table.ForeignKey(
                        name: "fk_tenant_usage_counters_platform_feature_id_platform_features",
                        column: x => x.platform_feature_id,
                        principalTable: "platform_features",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_tenant_usage_counters_tenant_id_tenants",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "platform_refresh_tokens",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false),
                    platform_auth_session_id = table.Column<Guid>(type: "uuid", nullable: false),
                    token_hash = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_platform_refresh_tokens", x => x.id);
                    table.CheckConstraint("ck_platform_refresh_tokens_status", "status IN ('ACTIVE', 'USED', 'EXPIRED', 'REVOKED')");
                    table.ForeignKey(
                        name: "fk_platform_refresh_tokens_platform_auth_session_id_platform_auth_sessions",
                        column: x => x.platform_auth_session_id,
                        principalTable: "platform_auth_sessions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "receipt_template_assignments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    outlet_id = table.Column<Guid>(type: "uuid", nullable: false),
                    till_id = table.Column<Guid>(type: "uuid", nullable: false),
                    pos_device_id = table.Column<Guid>(type: "uuid", nullable: false),
                    assignment_scope = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: false),
                    receipt_template_version_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_receipt_template_assignments", x => x.id);
                    table.CheckConstraint("ck_receipt_template_assignments_assignment_scope_outlet_id_til~", "(assignment_scope = 'OUTLET' AND outlet_id IS NOT NULL) OR (assignment_scope = 'TILL' AND till_id IS NOT NULL) OR (assignment_scope = 'POS_DEVICE' AND pos_device_id IS NOT NULL)");
                    table.ForeignKey(
                        name: "fk_receipt_template_assignments_receipt_template_version_id_receipt_template_versions",
                        column: x => x.receipt_template_version_id,
                        principalTable: "receipt_template_versions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "role_template_version_permissions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    permission_definition_id = table.Column<Guid>(type: "uuid", nullable: false),
                    role_template_version_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_role_template_version_permissions", x => x.id);
                    table.ForeignKey(
                        name: "fk_role_template_version_permissions_permission_definition_id_permission_definitions",
                        column: x => x.permission_definition_id,
                        principalTable: "permission_definitions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_role_template_version_permissions_role_template_version_id_role_template_versions",
                        column: x => x.role_template_version_id,
                        principalTable: "role_template_versions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "tenant_roles",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    role_code = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: false),
                    name = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    status = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false),
                    role_template_version_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tenant_roles", x => x.id);
                    table.CheckConstraint("ck_tenant_roles_status", "status IN ('ACTIVE', 'INACTIVE', 'DELETED')");
                    table.ForeignKey(
                        name: "fk_tenant_roles_role_template_version_id_role_template_versions",
                        column: x => x.role_template_version_id,
                        principalTable: "role_template_versions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_tenant_roles_tenant_id_tenants",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "choice_options",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false),
                    choice_group_id = table.Column<Guid>(type: "uuid", nullable: false),
                    option_code = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_choice_options", x => x.id);
                    table.CheckConstraint("ck_choice_options_sort_order", "sort_order >= 0");
                    table.ForeignKey(
                        name: "fk_choice_options_choice_group_id_choice_groups",
                        column: x => x.choice_group_id,
                        principalTable: "choice_groups",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "customer_auth_accounts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    customer_id = table.Column<Guid>(type: "uuid", nullable: true),
                    failed_login_count = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_customer_auth_accounts", x => x.id);
                    table.CheckConstraint("ck_customer_auth_accounts_failed_login_count", "failed_login_count >= 0");
                    table.ForeignKey(
                        name: "fk_customer_auth_accounts_customer_id_customers",
                        column: x => x.customer_id,
                        principalTable: "customers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "customer_consents",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    customer_id = table.Column<Guid>(type: "uuid", nullable: true),
                    consent_type = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: false),
                    sales_channel_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_customer_consents", x => x.id);
                    table.ForeignKey(
                        name: "fk_customer_consents_customer_id_customers",
                        column: x => x.customer_id,
                        principalTable: "customers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "customer_verification_otps",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    customer_id = table.Column<Guid>(type: "uuid", nullable: true),
                    status = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false),
                    attempt_count = table.Column<int>(type: "integer", nullable: false),
                    max_attempts = table.Column<int>(type: "integer", nullable: false),
                    normalized_recipient_value = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false),
                    verification_purpose = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_customer_verification_otps", x => x.id);
                    table.CheckConstraint("ck_customer_verification_otps_attempt_count", "attempt_count >= 0");
                    table.CheckConstraint("ck_customer_verification_otps_max_attempts", "max_attempts > 0");
                    table.ForeignKey(
                        name: "fk_customer_verification_otps_customer_id_customers",
                        column: x => x.customer_id,
                        principalTable: "customers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "discount_policies",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false),
                    discount_code = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: false),
                    discount_type_id = table.Column<Guid>(type: "uuid", nullable: false),
                    discount_value = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_discount_policies", x => x.id);
                    table.CheckConstraint("ck_discount_policies_discount_value", "discount_value >= 0");
                    table.ForeignKey(
                        name: "fk_discount_policies_discount_type_id_discount_types",
                        column: x => x.discount_type_id,
                        principalTable: "discount_types",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_discount_policies_tenant_id_tenants",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "expiry_discount_rule_tiers",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    days_before_expiry = table.Column<int>(type: "integer", nullable: false),
                    discount_value = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    expiry_discount_rule_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_expiry_discount_rule_tiers", x => x.id);
                    table.CheckConstraint("ck_expiry_discount_rule_tiers_days_before_expiry", "days_before_expiry >= 0");
                    table.CheckConstraint("ck_expiry_discount_rule_tiers_discount_value", "discount_value >= 0");
                    table.ForeignKey(
                        name: "fk_expiry_discount_rule_tiers_expiry_discount_rule_id_expiry_discount_rules",
                        column: x => x.expiry_discount_rule_id,
                        principalTable: "expiry_discount_rules",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "fulfillment_method_outlets",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    outlet_id = table.Column<Guid>(type: "uuid", nullable: true),
                    status = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false),
                    fulfillment_method_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_fulfillment_method_outlets", x => x.id);
                    table.ForeignKey(
                        name: "fk_fulfillment_method_outlets_fulfillment_method_id_fulfillment_methods",
                        column: x => x.fulfillment_method_id,
                        principalTable: "fulfillment_methods",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_fulfillment_method_outlets_outlet_id_outlets",
                        column: x => x.outlet_id,
                        principalTable: "outlets",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "hardware_devices",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    outlet_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false),
                    status = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false),
                    hardware_device_code = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: false),
                    serial_number = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_hardware_devices", x => x.id);
                    table.ForeignKey(
                        name: "fk_hardware_devices_outlet_id_outlets",
                        column: x => x.outlet_id,
                        principalTable: "outlets",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "inventory_locations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    outlet_id = table.Column<Guid>(type: "uuid", nullable: true),
                    name = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false),
                    location_code = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_inventory_locations", x => x.id);
                    table.ForeignKey(
                        name: "fk_inventory_locations_outlet_id_outlets",
                        column: x => x.outlet_id,
                        principalTable: "outlets",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_inventory_locations_tenant_id_tenants",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "outlet_addresses",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    outlet_id = table.Column<Guid>(type: "uuid", nullable: true),
                    address_type = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_outlet_addresses", x => x.id);
                    table.CheckConstraint("ck_outlet_addresses_address_type", "address_type IN ('PHYSICAL', 'BILLING', 'PICKUP')");
                    table.ForeignKey(
                        name: "fk_outlet_addresses_outlet_id_outlets",
                        column: x => x.outlet_id,
                        principalTable: "outlets",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "outlet_business_hours",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    outlet_id = table.Column<Guid>(type: "uuid", nullable: true),
                    day_of_week = table.Column<int>(type: "integer", nullable: false),
                    open_time = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    close_time = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_outlet_business_hours", x => x.id);
                    table.CheckConstraint("ck_outlet_business_hours_day_of_week", "day_of_week BETWEEN 0 AND 6");
                    table.CheckConstraint("ck_outlet_business_hours_open_time_close_time", "open_time < close_time");
                    table.ForeignKey(
                        name: "fk_outlet_business_hours_outlet_id_outlets",
                        column: x => x.outlet_id,
                        principalTable: "outlets",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "pos_devices",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    outlet_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false),
                    status = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false),
                    device_code = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: false),
                    device_serial_number = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_pos_devices", x => x.id);
                    table.ForeignKey(
                        name: "fk_pos_devices_outlet_id_outlets",
                        column: x => x.outlet_id,
                        principalTable: "outlets",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "tills",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    outlet_id = table.Column<Guid>(type: "uuid", nullable: true),
                    name = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false),
                    status = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false),
                    till_code = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tills", x => x.id);
                    table.CheckConstraint("ck_tills_status", "status IN ('ACTIVE', 'INACTIVE', 'DELETED')");
                    table.ForeignKey(
                        name: "fk_tills_outlet_id_outlets",
                        column: x => x.outlet_id,
                        principalTable: "outlets",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "price_list_outlets",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    outlet_id = table.Column<Guid>(type: "uuid", nullable: true),
                    status = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false),
                    price_list_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_price_list_outlets", x => x.id);
                    table.ForeignKey(
                        name: "fk_price_list_outlets_outlet_id_outlets",
                        column: x => x.outlet_id,
                        principalTable: "outlets",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_price_list_outlets_price_list_id_price_lists",
                        column: x => x.price_list_id,
                        principalTable: "price_lists",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "product_attribute_options",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false),
                    attribute_definition_id = table.Column<Guid>(type: "uuid", nullable: false),
                    option_code = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_product_attribute_options", x => x.id);
                    table.ForeignKey(
                        name: "fk_product_attribute_options_attribute_definition_id_product_attribute_definitions",
                        column: x => x.attribute_definition_id,
                        principalTable: "product_attribute_definitions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "business_type_option_templates",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    status = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false),
                    business_type_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_option_template_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_business_type_option_templates", x => x.id);
                    table.ForeignKey(
                        name: "fk_business_type_option_templates_business_type_id_business_types",
                        column: x => x.business_type_id,
                        principalTable: "business_types",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_business_type_option_templates_product_option_template_id_product_option_templates",
                        column: x => x.product_option_template_id,
                        principalTable: "product_option_templates",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "product_option_template_values",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false),
                    product_option_template_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    value_code = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_product_option_template_values", x => x.id);
                    table.CheckConstraint("ck_product_option_template_values_sort_order", "sort_order >= 0");
                    table.ForeignKey(
                        name: "fk_product_option_template_values_product_option_template_id_product_option_templates",
                        column: x => x.product_option_template_id,
                        principalTable: "product_option_templates",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "combo_definitions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false),
                    status = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false),
                    combo_code = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_combo_definitions", x => x.id);
                    table.CheckConstraint("ck_combo_definitions_status", "status IN ('ACTIVE', 'INACTIVE', 'DELETED')");
                    table.ForeignKey(
                        name: "fk_combo_definitions_product_id_products",
                        column: x => x.product_id,
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "inventory_reservation_lines",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    inventory_reservation_id = table.Column<Guid>(type: "uuid", nullable: false),
                    line_number = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    requested_quantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_inventory_reservation_lines", x => x.id);
                    table.CheckConstraint("ck_inventory_reservation_lines_requested_quantity", "requested_quantity > 0");
                    table.ForeignKey(
                        name: "fk_inventory_reservation_lines_inventory_reservation_id_inventory_reservations",
                        column: x => x.inventory_reservation_id,
                        principalTable: "inventory_reservations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_inventory_reservation_lines_product_id_products",
                        column: x => x.product_id,
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "product_attribute_values",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    attribute_definition_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_product_attribute_values", x => x.id);
                    table.ForeignKey(
                        name: "fk_product_attribute_values_attribute_definition_id_product_attribute_definitions",
                        column: x => x.attribute_definition_id,
                        principalTable: "product_attribute_definitions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_product_attribute_values_product_id_products",
                        column: x => x.product_id,
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "product_categories",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    category_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_product_categories", x => x.id);
                    table.ForeignKey(
                        name: "fk_product_categories_category_id_categories",
                        column: x => x.category_id,
                        principalTable: "categories",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_product_categories_product_id_products",
                        column: x => x.product_id,
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "product_choice_groups",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    choice_group_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_product_choice_groups", x => x.id);
                    table.ForeignKey(
                        name: "fk_product_choice_groups_choice_group_id_choice_groups",
                        column: x => x.choice_group_id,
                        principalTable: "choice_groups",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_product_choice_groups_product_id_products",
                        column: x => x.product_id,
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "product_collections",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    collection_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_product_collections", x => x.id);
                    table.ForeignKey(
                        name: "fk_product_collections_collection_id_collections",
                        column: x => x.collection_id,
                        principalTable: "collections",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_product_collections_product_id_products",
                        column: x => x.product_id,
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "product_images",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    image_url = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_product_images", x => x.id);
                    table.CheckConstraint("ck_product_images_sort_order", "sort_order >= 0");
                    table.ForeignKey(
                        name: "fk_product_images_product_id_products",
                        column: x => x.product_id,
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "product_options",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false),
                    option_code = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_option_template_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_product_options", x => x.id);
                    table.CheckConstraint("ck_product_options_sort_order", "sort_order >= 0");
                    table.ForeignKey(
                        name: "fk_product_options_product_id_products",
                        column: x => x.product_id,
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_product_options_product_option_template_id_product_option_templates",
                        column: x => x.product_option_template_id,
                        principalTable: "product_option_templates",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "product_variants",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false),
                    status = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sku = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false),
                    variant_code = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_product_variants", x => x.id);
                    table.ForeignKey(
                        name: "fk_product_variants_product_id_products",
                        column: x => x.product_id,
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "serial_numbers",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    serial_number = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_serial_numbers", x => x.id);
                    table.ForeignKey(
                        name: "fk_serial_numbers_product_id_products",
                        column: x => x.product_id,
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "price_list_channels",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false),
                    price_list_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sales_channel_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_price_list_channels", x => x.id);
                    table.ForeignKey(
                        name: "fk_price_list_channels_price_list_id_price_lists",
                        column: x => x.price_list_id,
                        principalTable: "price_lists",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_price_list_channels_sales_channel_id_sales_channels",
                        column: x => x.sales_channel_id,
                        principalTable: "sales_channels",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "product_channel_visibility",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sales_channel_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_product_channel_visibility", x => x.id);
                    table.ForeignKey(
                        name: "fk_product_channel_visibility_product_id_products",
                        column: x => x.product_id,
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_product_channel_visibility_sales_channel_id_sales_channels",
                        column: x => x.sales_channel_id,
                        principalTable: "sales_channels",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "pos_order_holds",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    hold_number = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: false),
                    sales_order_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_pos_order_holds", x => x.id);
                    table.ForeignKey(
                        name: "fk_pos_order_holds_sales_order_id_sales_orders",
                        column: x => x.sales_order_id,
                        principalTable: "sales_orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "receipts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    document_number_sequence_id = table.Column<Guid>(type: "uuid", nullable: false),
                    receipt_number = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: false),
                    sales_order_id = table.Column<Guid>(type: "uuid", nullable: true),
                    total_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_receipts", x => x.id);
                    table.CheckConstraint("ck_receipts_total_amount", "total_amount >= 0");
                    table.ForeignKey(
                        name: "fk_receipts_document_number_sequence_id_document_number_sequences",
                        column: x => x.document_number_sequence_id,
                        principalTable: "document_number_sequences",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_receipts_sales_order_id_sales_orders",
                        column: x => x.sales_order_id,
                        principalTable: "sales_orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "sales_order_lines",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    line_number = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: false),
                    line_total_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    quantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    sales_order_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sales_order_lines", x => x.id);
                    table.CheckConstraint("ck_sales_order_lines_line_total_amount", "line_total_amount >= 0");
                    table.CheckConstraint("ck_sales_order_lines_quantity", "quantity > 0");
                    table.ForeignKey(
                        name: "fk_sales_order_lines_sales_order_id_sales_orders",
                        column: x => x.sales_order_id,
                        principalTable: "sales_orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "sales_order_status_history",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    sales_order_id = table.Column<Guid>(type: "uuid", nullable: true),
                    sequence_number = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sales_order_status_history", x => x.id);
                    table.CheckConstraint("ck_sales_order_status_history_sequence_number", "sequence_number > 0");
                    table.ForeignKey(
                        name: "fk_sales_order_status_history_sales_order_id_sales_orders",
                        column: x => x.sales_order_id,
                        principalTable: "sales_orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "sales_payments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false),
                    paid_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    payment_method_id = table.Column<Guid>(type: "uuid", nullable: false),
                    payment_number = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: false),
                    requested_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    sales_order_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sales_payments", x => x.id);
                    table.CheckConstraint("ck_sales_payments_paid_amount", "paid_amount >= 0");
                    table.CheckConstraint("ck_sales_payments_requested_amount", "requested_amount >= 0");
                    table.ForeignKey(
                        name: "fk_sales_payments_payment_method_id_payment_methods",
                        column: x => x.payment_method_id,
                        principalTable: "payment_methods",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_sales_payments_sales_order_id_sales_orders",
                        column: x => x.sales_order_id,
                        principalTable: "sales_orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "sales_returns",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false),
                    return_number = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: false),
                    sales_order_id = table.Column<Guid>(type: "uuid", nullable: true),
                    total_refund_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    total_return_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sales_returns", x => x.id);
                    table.CheckConstraint("ck_sales_returns_total_refund_amount", "total_refund_amount >= 0");
                    table.CheckConstraint("ck_sales_returns_total_return_amount", "total_return_amount >= 0");
                    table.ForeignKey(
                        name: "fk_sales_returns_sales_order_id_sales_orders",
                        column: x => x.sales_order_id,
                        principalTable: "sales_orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "checkout_sessions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false),
                    checkout_number = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: false),
                    shopping_cart_id = table.Column<Guid>(type: "uuid", nullable: false),
                    total_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_checkout_sessions", x => x.id);
                    table.CheckConstraint("ck_checkout_sessions_total_amount", "total_amount >= 0");
                    table.ForeignKey(
                        name: "fk_checkout_sessions_shopping_cart_id_shopping_carts",
                        column: x => x.shopping_cart_id,
                        principalTable: "shopping_carts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "shopping_cart_items",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    line_number = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: false),
                    quantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    shopping_cart_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_shopping_cart_items", x => x.id);
                    table.CheckConstraint("ck_shopping_cart_items_quantity", "quantity > 0");
                    table.ForeignKey(
                        name: "fk_shopping_cart_items_shopping_cart_id_shopping_carts",
                        column: x => x.shopping_cart_id,
                        principalTable: "shopping_carts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "stock_adjustment_lines",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    adjustment_quantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    line_number = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    stock_adjustment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_stock_adjustment_lines", x => x.id);
                    table.CheckConstraint("ck_stock_adjustment_lines_adjustment_quantity", "adjustment_quantity <> 0");
                    table.ForeignKey(
                        name: "fk_stock_adjustment_lines_product_id_products",
                        column: x => x.product_id,
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_stock_adjustment_lines_stock_adjustment_id_stock_adjustments",
                        column: x => x.stock_adjustment_id,
                        principalTable: "stock_adjustments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "stock_movement_references",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    reference_type = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: false),
                    reference_id = table.Column<Guid>(type: "uuid", nullable: false),
                    stock_movement_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_stock_movement_references", x => x.id);
                    table.ForeignKey(
                        name: "fk_stock_movement_references_stock_movement_id_stock_movements",
                        column: x => x.stock_movement_id,
                        principalTable: "stock_movements",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "product_tax_assignments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_variant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tax_class_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_product_tax_assignments", x => x.id);
                    table.ForeignKey(
                        name: "fk_product_tax_assignments_product_id_products",
                        column: x => x.product_id,
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_product_tax_assignments_tax_class_id_tax_classes",
                        column: x => x.tax_class_id,
                        principalTable: "tax_classes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "tax_rates",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false),
                    rate_percent = table.Column<decimal>(type: "numeric(9,4)", precision: 9, scale: 4, nullable: false),
                    tax_jurisdiction_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tax_rate_code = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tax_rates", x => x.id);
                    table.CheckConstraint("ck_tax_rates_rate_percent", "rate_percent >= 0");
                    table.ForeignKey(
                        name: "fk_tax_rates_tax_jurisdiction_id_tax_jurisdictions",
                        column: x => x.tax_jurisdiction_id,
                        principalTable: "tax_jurisdictions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "subscription_invoices",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    invoice_number = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: false),
                    tenant_subscription_id = table.Column<Guid>(type: "uuid", nullable: false),
                    total_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_subscription_invoices", x => x.id);
                    table.CheckConstraint("ck_subscription_invoices_total_amount", "total_amount >= 0");
                    table.ForeignKey(
                        name: "fk_subscription_invoices_tenant_id_tenants",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_subscription_invoices_tenant_subscription_id_tenant_subscriptions",
                        column: x => x.tenant_subscription_id,
                        principalTable: "tenant_subscriptions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "tenant_subscription_addons",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    status = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false),
                    subscription_addon_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_subscription_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tenant_subscription_addons", x => x.id);
                    table.ForeignKey(
                        name: "fk_tenant_subscription_addons_subscription_addon_id_subscription_addons",
                        column: x => x.subscription_addon_id,
                        principalTable: "subscription_addons",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_tenant_subscription_addons_tenant_subscription_id_tenant_subscriptions",
                        column: x => x.tenant_subscription_id,
                        principalTable: "tenant_subscriptions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "tenant_subscription_history",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    sequence_number = table.Column<int>(type: "integer", nullable: false),
                    tenant_subscription_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tenant_subscription_history", x => x.id);
                    table.CheckConstraint("ck_tenant_subscription_history_sequence_number", "sequence_number > 0");
                    table.ForeignKey(
                        name: "fk_tenant_subscription_history_tenant_subscription_id_tenant_subscriptions",
                        column: x => x.tenant_subscription_id,
                        principalTable: "tenant_subscriptions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "email_verification_tokens",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    status = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false),
                    token_hash = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_email_verification_tokens", x => x.id);
                    table.CheckConstraint("ck_email_verification_tokens_status", "status IN ('PENDING', 'VERIFIED', 'EXPIRED', 'REVOKED')");
                    table.ForeignKey(
                        name: "fk_email_verification_tokens_tenant_user_id_tenant_users",
                        column: x => x.tenant_user_id,
                        principalTable: "tenant_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "outlet_user_permissions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    outlet_id = table.Column<Guid>(type: "uuid", nullable: true),
                    description = table.Column<string>(type: "text", nullable: true),
                    permission_definition_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_outlet_user_permissions", x => x.id);
                    table.ForeignKey(
                        name: "fk_outlet_user_permissions_outlet_id_outlets",
                        column: x => x.outlet_id,
                        principalTable: "outlets",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_outlet_user_permissions_permission_definition_id_permission_definitions",
                        column: x => x.permission_definition_id,
                        principalTable: "permission_definitions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_outlet_user_permissions_tenant_user_id_tenant_users",
                        column: x => x.tenant_user_id,
                        principalTable: "tenant_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "password_reset_tokens",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    status = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false),
                    token_hash = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_password_reset_tokens", x => x.id);
                    table.CheckConstraint("ck_password_reset_tokens_status", "status IN ('PENDING', 'USED', 'EXPIRED', 'REVOKED')");
                    table.ForeignKey(
                        name: "fk_password_reset_tokens_tenant_user_id_tenant_users",
                        column: x => x.tenant_user_id,
                        principalTable: "tenant_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "tenant_auth_sessions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    status = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false),
                    session_token_hash = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tenant_auth_sessions", x => x.id);
                    table.CheckConstraint("ck_tenant_auth_sessions_status", "status IN ('ACTIVE', 'EXPIRED', 'REVOKED')");
                    table.ForeignKey(
                        name: "fk_tenant_auth_sessions_tenant_user_id_tenant_users",
                        column: x => x.tenant_user_id,
                        principalTable: "tenant_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "tenant_login_audits",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    login_result = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tenant_login_audits", x => x.id);
                    table.CheckConstraint("ck_tenant_login_audits_login_result", "login_result IN ('SUCCESS', 'FAILED', 'LOCKED')");
                    table.ForeignKey(
                        name: "fk_tenant_login_audits_tenant_user_id_tenant_users",
                        column: x => x.tenant_user_id,
                        principalTable: "tenant_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "tenant_user_permissions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    description = table.Column<string>(type: "text", nullable: true),
                    permission_definition_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tenant_user_permissions", x => x.id);
                    table.ForeignKey(
                        name: "fk_tenant_user_permissions_permission_definition_id_permission_definitions",
                        column: x => x.permission_definition_id,
                        principalTable: "permission_definitions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_tenant_user_permissions_tenant_user_id_tenant_users",
                        column: x => x.tenant_user_id,
                        principalTable: "tenant_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "user_setup_tokens",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false),
                    token_hash = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false),
                    user_invite_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_setup_tokens", x => x.id);
                    table.CheckConstraint("ck_user_setup_tokens_status", "status IN ('PENDING', 'USED', 'EXPIRED', 'REVOKED')");
                    table.ForeignKey(
                        name: "fk_user_setup_tokens_user_invite_id_user_invites",
                        column: x => x.user_invite_id,
                        principalTable: "user_invites",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "notification_messages",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    platform_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    customer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    recipient_type = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: false),
                    message_number = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: false),
                    notification_channel_id = table.Column<Guid>(type: "uuid", nullable: false),
                    notification_event_id = table.Column<Guid>(type: "uuid", nullable: false),
                    notification_template_version_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_notification_messages", x => x.id);
                    table.CheckConstraint("ck_notification_messages_recipient_type_platform_user_id_tenan~", "(recipient_type = 'PLATFORM_USER' AND platform_user_id IS NOT NULL) OR (recipient_type = 'TENANT_USER' AND tenant_user_id IS NOT NULL) OR (recipient_type = 'CUSTOMER' AND customer_id IS NOT NULL)");
                    table.ForeignKey(
                        name: "fk_notification_messages_notification_channel_id_notification_channels",
                        column: x => x.notification_channel_id,
                        principalTable: "notification_channels",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_notification_messages_notification_event_id_notification_events",
                        column: x => x.notification_event_id,
                        principalTable: "notification_events",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_notification_messages_notification_template_version_id_notification_template_versions",
                        column: x => x.notification_template_version_id,
                        principalTable: "notification_template_versions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "subscription_addon_limits",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    feature_limit_definition_id = table.Column<Guid>(type: "uuid", nullable: false),
                    limit_value = table.Column<int>(type: "integer", nullable: true),
                    subscription_addon_feature_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_subscription_addon_limits", x => x.id);
                    table.CheckConstraint("ck_subscription_addon_limits_limit_value", "limit_value IS NULL OR limit_value >= 0");
                    table.ForeignKey(
                        name: "fk_subscription_addon_limits_feature_limit_definition_id_feature_limit_definitions",
                        column: x => x.feature_limit_definition_id,
                        principalTable: "feature_limit_definitions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_subscription_addon_limits_subscription_addon_feature_id_subscription_addon_features",
                        column: x => x.subscription_addon_feature_id,
                        principalTable: "subscription_addon_features",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "subscription_plan_feature_limits",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    feature_limit_definition_id = table.Column<Guid>(type: "uuid", nullable: false),
                    limit_value = table.Column<int>(type: "integer", nullable: true),
                    subscription_plan_feature_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_subscription_plan_feature_limits", x => x.id);
                    table.CheckConstraint("ck_subscription_plan_feature_limits_limit_value", "limit_value IS NULL OR limit_value >= 0");
                    table.ForeignKey(
                        name: "fk_subscription_plan_feature_limits_feature_limit_definition_id_feature_limit_definitions",
                        column: x => x.feature_limit_definition_id,
                        principalTable: "feature_limit_definitions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_subscription_plan_feature_limits_subscription_plan_feature_id_subscription_plan_features",
                        column: x => x.subscription_plan_feature_id,
                        principalTable: "subscription_plan_features",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "outlet_user_roles",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    outlet_id = table.Column<Guid>(type: "uuid", nullable: true),
                    description = table.Column<string>(type: "text", nullable: true),
                    tenant_role_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_outlet_user_roles", x => x.id);
                    table.ForeignKey(
                        name: "fk_outlet_user_roles_outlet_id_outlets",
                        column: x => x.outlet_id,
                        principalTable: "outlets",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_outlet_user_roles_tenant_role_id_tenant_roles",
                        column: x => x.tenant_role_id,
                        principalTable: "tenant_roles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_outlet_user_roles_tenant_user_id_tenant_users",
                        column: x => x.tenant_user_id,
                        principalTable: "tenant_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "tenant_role_permissions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    permission_definition_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_role_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tenant_role_permissions", x => x.id);
                    table.ForeignKey(
                        name: "fk_tenant_role_permissions_permission_definition_id_permission_definitions",
                        column: x => x.permission_definition_id,
                        principalTable: "permission_definitions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_tenant_role_permissions_tenant_role_id_tenant_roles",
                        column: x => x.tenant_role_id,
                        principalTable: "tenant_roles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "tenant_user_roles",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    description = table.Column<string>(type: "text", nullable: true),
                    tenant_role_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tenant_user_roles", x => x.id);
                    table.ForeignKey(
                        name: "fk_tenant_user_roles_tenant_role_id_tenant_roles",
                        column: x => x.tenant_role_id,
                        principalTable: "tenant_roles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_tenant_user_roles_tenant_user_id_tenant_users",
                        column: x => x.tenant_user_id,
                        principalTable: "tenant_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "customer_auth_sessions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false),
                    customer_auth_account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    session_token_hash = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_customer_auth_sessions", x => x.id);
                    table.ForeignKey(
                        name: "fk_customer_auth_sessions_customer_auth_account_id_customer_auth_accounts",
                        column: x => x.customer_auth_account_id,
                        principalTable: "customer_auth_accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "customer_password_reset_tokens",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    customer_auth_account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    token_hash = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false),
                    verified_otp_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_customer_password_reset_tokens", x => x.id);
                    table.ForeignKey(
                        name: "fk_customer_password_reset_tokens_customer_auth_account_id_customer_auth_accounts",
                        column: x => x.customer_auth_account_id,
                        principalTable: "customer_auth_accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_customer_password_reset_tokens_verified_otp_id_customer_verification_otps",
                        column: x => x.verified_otp_id,
                        principalTable: "customer_verification_otps",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "discount_policy_channels",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false),
                    discount_policy_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sales_channel_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_discount_policy_channels", x => x.id);
                    table.ForeignKey(
                        name: "fk_discount_policy_channels_discount_policy_id_discount_policies",
                        column: x => x.discount_policy_id,
                        principalTable: "discount_policies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_discount_policy_channels_sales_channel_id_sales_channels",
                        column: x => x.sales_channel_id,
                        principalTable: "sales_channels",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "discount_policy_conditions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    condition_sequence = table.Column<int>(type: "integer", nullable: false),
                    discount_policy_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_discount_policy_conditions", x => x.id);
                    table.CheckConstraint("ck_discount_policy_conditions_condition_sequence", "condition_sequence > 0");
                    table.ForeignKey(
                        name: "fk_discount_policy_conditions_discount_policy_id_discount_policies",
                        column: x => x.discount_policy_id,
                        principalTable: "discount_policies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "discount_policy_outlets",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    outlet_id = table.Column<Guid>(type: "uuid", nullable: true),
                    status = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false),
                    discount_policy_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_discount_policy_outlets", x => x.id);
                    table.ForeignKey(
                        name: "fk_discount_policy_outlets_discount_policy_id_discount_policies",
                        column: x => x.discount_policy_id,
                        principalTable: "discount_policies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_discount_policy_outlets_outlet_id_outlets",
                        column: x => x.outlet_id,
                        principalTable: "outlets",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "discount_policy_targets",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    discount_policy_id = table.Column<Guid>(type: "uuid", nullable: false),
                    target_id = table.Column<Guid>(type: "uuid", nullable: false),
                    target_type = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_discount_policy_targets", x => x.id);
                    table.ForeignKey(
                        name: "fk_discount_policy_targets_discount_policy_id_discount_policies",
                        column: x => x.discount_policy_id,
                        principalTable: "discount_policies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "fulfillment_orders",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false),
                    fulfillment_method_outlet_id = table.Column<Guid>(type: "uuid", nullable: false),
                    fulfillment_number = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: false),
                    sales_order_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_fulfillment_orders", x => x.id);
                    table.ForeignKey(
                        name: "fk_fulfillment_orders_fulfillment_method_outlet_id_fulfillment_method_outlets",
                        column: x => x.fulfillment_method_outlet_id,
                        principalTable: "fulfillment_method_outlets",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_fulfillment_orders_sales_order_id_sales_orders",
                        column: x => x.sales_order_id,
                        principalTable: "sales_orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "pickup_slots",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    capacity = table.Column<int>(type: "integer", nullable: false),
                    fulfillment_method_outlet_id = table.Column<Guid>(type: "uuid", nullable: false),
                    reserved_count = table.Column<int>(type: "integer", nullable: false),
                    slot_date = table.Column<DateOnly>(type: "date", nullable: false),
                    window_end = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    window_start = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_pickup_slots", x => x.id);
                    table.CheckConstraint("ck_pickup_slots_capacity", "capacity >= 0");
                    table.CheckConstraint("ck_pickup_slots_reserved_count", "reserved_count >= 0");
                    table.CheckConstraint("ck_pickup_slots_reserved_count_capacity", "reserved_count <= capacity");
                    table.ForeignKey(
                        name: "fk_pickup_slots_fulfillment_method_outlet_id_fulfillment_method_outlets",
                        column: x => x.fulfillment_method_outlet_id,
                        principalTable: "fulfillment_method_outlets",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "hardware_test_logs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    hardware_device_id = table.Column<Guid>(type: "uuid", nullable: false),
                    test_result = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_hardware_test_logs", x => x.id);
                    table.CheckConstraint("ck_hardware_test_logs_test_result", "test_result IN ('SUCCESS', 'FAILED', 'WARNING')");
                    table.ForeignKey(
                        name: "fk_hardware_test_logs_hardware_device_id_hardware_devices",
                        column: x => x.hardware_device_id,
                        principalTable: "hardware_devices",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "inventory_balances",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    inventory_location_id = table.Column<Guid>(type: "uuid", nullable: false),
                    on_hand_quantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    product_batch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_variant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    reserved_quantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_inventory_balances", x => x.id);
                    table.CheckConstraint("ck_inventory_balances_on_hand_quantity", "on_hand_quantity >= 0");
                    table.CheckConstraint("ck_inventory_balances_reserved_quantity", "reserved_quantity >= 0");
                    table.ForeignKey(
                        name: "fk_inventory_balances_inventory_location_id_inventory_locations",
                        column: x => x.inventory_location_id,
                        principalTable: "inventory_locations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_inventory_balances_product_id_products",
                        column: x => x.product_id,
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "inventory_channel_allocations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    allocation_limit_quantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    inventory_location_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_variant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sales_channel_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_inventory_channel_allocations", x => x.id);
                    table.CheckConstraint("ck_inventory_channel_allocations_allocation_limit_quantity", "allocation_limit_quantity >= 0");
                    table.ForeignKey(
                        name: "fk_inventory_channel_allocations_inventory_location_id_inventory_locations",
                        column: x => x.inventory_location_id,
                        principalTable: "inventory_locations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_inventory_channel_allocations_sales_channel_id_sales_channels",
                        column: x => x.sales_channel_id,
                        principalTable: "sales_channels",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "inventory_reorder_rules",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    inventory_location_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_variant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    reorder_point_quantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_inventory_reorder_rules", x => x.id);
                    table.CheckConstraint("ck_inventory_reorder_rules_reorder_point_quantity", "reorder_point_quantity >= 0");
                    table.ForeignKey(
                        name: "fk_inventory_reorder_rules_inventory_location_id_inventory_locations",
                        column: x => x.inventory_location_id,
                        principalTable: "inventory_locations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_inventory_reorder_rules_product_id_products",
                        column: x => x.product_id,
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "stock_transfers",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false),
                    destination_inventory_location_id = table.Column<Guid>(type: "uuid", nullable: false),
                    source_inventory_location_id = table.Column<Guid>(type: "uuid", nullable: false),
                    transfer_number = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_stock_transfers", x => x.id);
                    table.CheckConstraint("ck_stock_transfers_source_inventory_location_id_destination_in~", "source_inventory_location_id <> destination_inventory_location_id");
                    table.ForeignKey(
                        name: "fk_stock_transfers_destination_inventory_location_id_inventory_locations",
                        column: x => x.destination_inventory_location_id,
                        principalTable: "inventory_locations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_stock_transfers_source_inventory_location_id_inventory_locations",
                        column: x => x.source_inventory_location_id,
                        principalTable: "inventory_locations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "stocktake_sessions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false),
                    inventory_location_id = table.Column<Guid>(type: "uuid", nullable: false),
                    stocktake_number = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_stocktake_sessions", x => x.id);
                    table.ForeignKey(
                        name: "fk_stocktake_sessions_inventory_location_id_inventory_locations",
                        column: x => x.inventory_location_id,
                        principalTable: "inventory_locations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "hardware_device_assignments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    pos_device_id = table.Column<Guid>(type: "uuid", nullable: true),
                    effective_from = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false),
                    hardware_device_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_hardware_device_assignments", x => x.id);
                    table.ForeignKey(
                        name: "fk_hardware_device_assignments_hardware_device_id_hardware_devices",
                        column: x => x.hardware_device_id,
                        principalTable: "hardware_devices",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_hardware_device_assignments_pos_device_id_pos_devices",
                        column: x => x.pos_device_id,
                        principalTable: "pos_devices",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "offline_clients",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    outlet_id = table.Column<Guid>(type: "uuid", nullable: true),
                    pos_device_id = table.Column<Guid>(type: "uuid", nullable: true),
                    name = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false),
                    status = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false),
                    client_code = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: false),
                    max_offline_duration_minutes = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_offline_clients", x => x.id);
                    table.CheckConstraint("ck_offline_clients_max_offline_duration_minutes", "max_offline_duration_minutes IS NULL OR max_offline_duration_minutes > 0");
                    table.ForeignKey(
                        name: "fk_offline_clients_outlet_id_outlets",
                        column: x => x.outlet_id,
                        principalTable: "outlets",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_offline_clients_pos_device_id_pos_devices",
                        column: x => x.pos_device_id,
                        principalTable: "pos_devices",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_offline_clients_tenant_id_tenants",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "till_device_assignments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    till_id = table.Column<Guid>(type: "uuid", nullable: true),
                    pos_device_id = table.Column<Guid>(type: "uuid", nullable: true),
                    effective_from = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_till_device_assignments", x => x.id);
                    table.ForeignKey(
                        name: "fk_till_device_assignments_pos_device_id_pos_devices",
                        column: x => x.pos_device_id,
                        principalTable: "pos_devices",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_till_device_assignments_till_id_tills",
                        column: x => x.till_id,
                        principalTable: "tills",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "till_sessions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    till_id = table.Column<Guid>(type: "uuid", nullable: true),
                    status = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false),
                    closing_cash_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    opened_by_tenant_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    session_number = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_till_sessions", x => x.id);
                    table.CheckConstraint("ck_till_sessions_closing_cash_amount", "closing_cash_amount IS NULL OR closing_cash_amount >= 0");
                    table.ForeignKey(
                        name: "fk_till_sessions_opened_by_tenant_user_id_tenant_users",
                        column: x => x.opened_by_tenant_user_id,
                        principalTable: "tenant_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_till_sessions_till_id_tills",
                        column: x => x.till_id,
                        principalTable: "tills",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "combo_components",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    combo_definition_id = table.Column<Guid>(type: "uuid", nullable: false),
                    component_product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    component_variant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    quantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_combo_components", x => x.id);
                    table.CheckConstraint("ck_combo_components_quantity", "quantity > 0");
                    table.ForeignKey(
                        name: "fk_combo_components_combo_definition_id_combo_definitions",
                        column: x => x.combo_definition_id,
                        principalTable: "combo_definitions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_combo_components_component_product_id_products",
                        column: x => x.component_product_id,
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "combo_groups",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false),
                    combo_definition_id = table.Column<Guid>(type: "uuid", nullable: false),
                    group_code = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: false),
                    max_select = table.Column<int>(type: "integer", nullable: false),
                    min_select = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_combo_groups", x => x.id);
                    table.CheckConstraint("ck_combo_groups_max_select_min_select", "max_select >= min_select");
                    table.CheckConstraint("ck_combo_groups_min_select", "min_select >= 0");
                    table.ForeignKey(
                        name: "fk_combo_groups_combo_definition_id_combo_definitions",
                        column: x => x.combo_definition_id,
                        principalTable: "combo_definitions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "product_attribute_value_options",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    attribute_option_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_attribute_value_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_product_attribute_value_options", x => x.id);
                    table.ForeignKey(
                        name: "fk_product_attribute_value_options_attribute_option_id_product_attribute_options",
                        column: x => x.attribute_option_id,
                        principalTable: "product_attribute_options",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_product_attribute_value_options_product_attribute_value_id_product_attribute_values",
                        column: x => x.product_attribute_value_id,
                        principalTable: "product_attribute_values",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "product_choice_options",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    choice_option_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_choice_group_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_product_choice_options", x => x.id);
                    table.ForeignKey(
                        name: "fk_product_choice_options_choice_option_id_choice_options",
                        column: x => x.choice_option_id,
                        principalTable: "choice_options",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_product_choice_options_product_choice_group_id_product_choice_groups",
                        column: x => x.product_choice_group_id,
                        principalTable: "product_choice_groups",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "product_option_values",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false),
                    option_value_code = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: false),
                    product_option_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_product_option_values", x => x.id);
                    table.CheckConstraint("ck_product_option_values_sort_order", "sort_order >= 0");
                    table.ForeignKey(
                        name: "fk_product_option_values_product_option_id_product_options",
                        column: x => x.product_option_id,
                        principalTable: "product_options",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "price_list_items",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    price_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    price_list_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_variant_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_price_list_items", x => x.id);
                    table.CheckConstraint("ck_price_list_items_price_amount", "price_amount >= 0");
                    table.ForeignKey(
                        name: "fk_price_list_items_price_list_id_price_lists",
                        column: x => x.price_list_id,
                        principalTable: "price_lists",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_price_list_items_product_id_products",
                        column: x => x.product_id,
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_price_list_items_product_variant_id_product_variants",
                        column: x => x.product_variant_id,
                        principalTable: "product_variants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "product_barcodes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    barcode_value = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_variant_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_product_barcodes", x => x.id);
                    table.ForeignKey(
                        name: "fk_product_barcodes_product_id_products",
                        column: x => x.product_id,
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_product_barcodes_product_variant_id_product_variants",
                        column: x => x.product_variant_id,
                        principalTable: "product_variants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "product_batches",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false),
                    batch_number = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: false),
                    manufactured_date = table.Column<DateOnly>(type: "date", nullable: true),
                    expiry_date = table.Column<DateOnly>(type: "date", nullable: true),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_variant_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_product_batches", x => x.id);
                    table.CheckConstraint("ck_product_batches_expiry_date_manufactured_date", "expiry_date IS NULL OR expiry_date >= manufactured_date");
                    table.ForeignKey(
                        name: "fk_product_batches_product_id_products",
                        column: x => x.product_id,
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_product_batches_product_variant_id_product_variants",
                        column: x => x.product_variant_id,
                        principalTable: "product_variants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "product_inventory_settings",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_variant_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_product_inventory_settings", x => x.id);
                    table.ForeignKey(
                        name: "fk_product_inventory_settings_product_id_products",
                        column: x => x.product_id,
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_product_inventory_settings_product_variant_id_product_variants",
                        column: x => x.product_variant_id,
                        principalTable: "product_variants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "stock_movement_serials",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    serial_number_id = table.Column<Guid>(type: "uuid", nullable: false),
                    stock_movement_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_stock_movement_serials", x => x.id);
                    table.ForeignKey(
                        name: "fk_stock_movement_serials_serial_number_id_serial_numbers",
                        column: x => x.serial_number_id,
                        principalTable: "serial_numbers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_stock_movement_serials_stock_movement_id_stock_movements",
                        column: x => x.stock_movement_id,
                        principalTable: "stock_movements",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "receipt_print_logs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    attempt_number = table.Column<int>(type: "integer", nullable: false),
                    receipt_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_receipt_print_logs", x => x.id);
                    table.CheckConstraint("ck_receipt_print_logs_attempt_number", "attempt_number > 0");
                    table.ForeignKey(
                        name: "fk_receipt_print_logs_receipt_id_receipts",
                        column: x => x.receipt_id,
                        principalTable: "receipts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "sales_order_charges",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    charge_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    sales_order_id = table.Column<Guid>(type: "uuid", nullable: true),
                    sales_order_line_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sales_order_charges", x => x.id);
                    table.CheckConstraint("ck_sales_order_charges_charge_amount", "charge_amount >= 0");
                    table.ForeignKey(
                        name: "fk_sales_order_charges_sales_order_id_sales_orders",
                        column: x => x.sales_order_id,
                        principalTable: "sales_orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_sales_order_charges_sales_order_line_id_sales_order_lines",
                        column: x => x.sales_order_line_id,
                        principalTable: "sales_order_lines",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "sales_order_discounts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    application_sequence = table.Column<int>(type: "integer", nullable: false),
                    discount_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    sales_order_id = table.Column<Guid>(type: "uuid", nullable: true),
                    sales_order_line_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sales_order_discounts", x => x.id);
                    table.CheckConstraint("ck_sales_order_discounts_application_sequence", "application_sequence > 0");
                    table.CheckConstraint("ck_sales_order_discounts_discount_amount", "discount_amount >= 0");
                    table.ForeignKey(
                        name: "fk_sales_order_discounts_sales_order_id_sales_orders",
                        column: x => x.sales_order_id,
                        principalTable: "sales_orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_sales_order_discounts_sales_order_line_id_sales_order_lines",
                        column: x => x.sales_order_line_id,
                        principalTable: "sales_order_lines",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "sales_order_line_components",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    quantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    sales_order_line_id = table.Column<Guid>(type: "uuid", nullable: true),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sales_order_line_components", x => x.id);
                    table.CheckConstraint("ck_sales_order_line_components_quantity", "quantity > 0");
                    table.CheckConstraint("ck_sales_order_line_components_sort_order", "sort_order >= 0");
                    table.ForeignKey(
                        name: "fk_sales_order_line_components_sales_order_line_id_sales_order_lines",
                        column: x => x.sales_order_line_id,
                        principalTable: "sales_order_lines",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "sales_order_line_options",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    quantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    sales_order_line_id = table.Column<Guid>(type: "uuid", nullable: true),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sales_order_line_options", x => x.id);
                    table.CheckConstraint("ck_sales_order_line_options_quantity", "quantity > 0");
                    table.CheckConstraint("ck_sales_order_line_options_sort_order", "sort_order >= 0");
                    table.ForeignKey(
                        name: "fk_sales_order_line_options_sales_order_line_id_sales_order_lines",
                        column: x => x.sales_order_line_id,
                        principalTable: "sales_order_lines",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "sales_order_line_status_history",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    sales_order_line_id = table.Column<Guid>(type: "uuid", nullable: true),
                    sequence_number = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sales_order_line_status_history", x => x.id);
                    table.CheckConstraint("ck_sales_order_line_status_history_sequence_number", "sequence_number > 0");
                    table.ForeignKey(
                        name: "fk_sales_order_line_status_history_sales_order_line_id_sales_order_lines",
                        column: x => x.sales_order_line_id,
                        principalTable: "sales_order_lines",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "sales_order_taxes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    sales_order_id = table.Column<Guid>(type: "uuid", nullable: true),
                    sales_order_line_id = table.Column<Guid>(type: "uuid", nullable: true),
                    tax_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    tax_rate_percent = table.Column<decimal>(type: "numeric(9,4)", precision: 9, scale: 4, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sales_order_taxes", x => x.id);
                    table.CheckConstraint("ck_sales_order_taxes_tax_amount", "tax_amount >= 0");
                    table.CheckConstraint("ck_sales_order_taxes_tax_rate_percent", "tax_rate_percent >= 0");
                    table.ForeignKey(
                        name: "fk_sales_order_taxes_sales_order_id_sales_orders",
                        column: x => x.sales_order_id,
                        principalTable: "sales_orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_sales_order_taxes_sales_order_line_id_sales_order_lines",
                        column: x => x.sales_order_line_id,
                        principalTable: "sales_order_lines",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "sales_payment_events",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    sales_payment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sequence_number = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sales_payment_events", x => x.id);
                    table.CheckConstraint("ck_sales_payment_events_sequence_number", "sequence_number > 0");
                    table.ForeignKey(
                        name: "fk_sales_payment_events_sales_payment_id_sales_payments",
                        column: x => x.sales_payment_id,
                        principalTable: "sales_payments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "sales_payment_transactions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    idempotency_key = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: false),
                    sales_payment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sales_payment_transactions", x => x.id);
                    table.CheckConstraint("ck_sales_payment_transactions_amount", "amount >= 0");
                    table.ForeignKey(
                        name: "fk_sales_payment_transactions_sales_payment_id_sales_payments",
                        column: x => x.sales_payment_id,
                        principalTable: "sales_payments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "sales_exchanges",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false),
                    additional_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    exchange_number = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: false),
                    refund_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    replacement_order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sales_return_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sales_exchanges", x => x.id);
                    table.CheckConstraint("ck_sales_exchanges_additional_amount", "additional_amount >= 0");
                    table.CheckConstraint("ck_sales_exchanges_refund_amount", "refund_amount >= 0");
                    table.ForeignKey(
                        name: "fk_sales_exchanges_replacement_order_id_sales_orders",
                        column: x => x.replacement_order_id,
                        principalTable: "sales_orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_sales_exchanges_sales_return_id_sales_returns",
                        column: x => x.sales_return_id,
                        principalTable: "sales_returns",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "sales_refunds",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false),
                    refund_number = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: false),
                    refunded_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    requested_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    sales_order_id = table.Column<Guid>(type: "uuid", nullable: true),
                    sales_return_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sales_refunds", x => x.id);
                    table.CheckConstraint("ck_sales_refunds_refunded_amount", "refunded_amount >= 0");
                    table.CheckConstraint("ck_sales_refunds_requested_amount", "requested_amount >= 0");
                    table.ForeignKey(
                        name: "fk_sales_refunds_sales_order_id_sales_orders",
                        column: x => x.sales_order_id,
                        principalTable: "sales_orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_sales_refunds_sales_return_id_sales_returns",
                        column: x => x.sales_return_id,
                        principalTable: "sales_returns",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "sales_return_events",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    sales_return_id = table.Column<Guid>(type: "uuid", nullable: true),
                    sequence_number = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sales_return_events", x => x.id);
                    table.CheckConstraint("ck_sales_return_events_sequence_number", "sequence_number > 0");
                    table.ForeignKey(
                        name: "fk_sales_return_events_sales_return_id_sales_returns",
                        column: x => x.sales_return_id,
                        principalTable: "sales_returns",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "sales_return_lines",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    requested_quantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    sales_order_line_id = table.Column<Guid>(type: "uuid", nullable: true),
                    sales_return_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sales_return_lines", x => x.id);
                    table.CheckConstraint("ck_sales_return_lines_requested_quantity", "requested_quantity > 0");
                    table.ForeignKey(
                        name: "fk_sales_return_lines_sales_order_line_id_sales_order_lines",
                        column: x => x.sales_order_line_id,
                        principalTable: "sales_order_lines",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_sales_return_lines_sales_return_id_sales_returns",
                        column: x => x.sales_return_id,
                        principalTable: "sales_returns",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "checkout_events",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    checkout_session_id = table.Column<Guid>(type: "uuid", nullable: true),
                    sequence_number = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_checkout_events", x => x.id);
                    table.CheckConstraint("ck_checkout_events_sequence_number", "sequence_number > 0");
                    table.ForeignKey(
                        name: "fk_checkout_events_checkout_session_id_checkout_sessions",
                        column: x => x.checkout_session_id,
                        principalTable: "checkout_sessions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "checkout_session_lines",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    checkout_session_id = table.Column<Guid>(type: "uuid", nullable: true),
                    line_number = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: false),
                    quantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_checkout_session_lines", x => x.id);
                    table.CheckConstraint("ck_checkout_session_lines_quantity", "quantity > 0");
                    table.ForeignKey(
                        name: "fk_checkout_session_lines_checkout_session_id_checkout_sessions",
                        column: x => x.checkout_session_id,
                        principalTable: "checkout_sessions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "shopping_cart_item_components",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    quantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    shopping_cart_item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_shopping_cart_item_components", x => x.id);
                    table.CheckConstraint("ck_shopping_cart_item_components_quantity", "quantity > 0");
                    table.ForeignKey(
                        name: "fk_shopping_cart_item_components_shopping_cart_item_id_shopping_cart_items",
                        column: x => x.shopping_cart_item_id,
                        principalTable: "shopping_cart_items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "shopping_cart_item_options",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    quantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    shopping_cart_item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_shopping_cart_item_options", x => x.id);
                    table.CheckConstraint("ck_shopping_cart_item_options_quantity", "quantity > 0");
                    table.ForeignKey(
                        name: "fk_shopping_cart_item_options_shopping_cart_item_id_shopping_cart_items",
                        column: x => x.shopping_cart_item_id,
                        principalTable: "shopping_cart_items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "tax_class_rates",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tax_class_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tax_rate_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tax_class_rates", x => x.id);
                    table.ForeignKey(
                        name: "fk_tax_class_rates_tax_class_id_tax_classes",
                        column: x => x.tax_class_id,
                        principalTable: "tax_classes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_tax_class_rates_tax_rate_id_tax_rates",
                        column: x => x.tax_rate_id,
                        principalTable: "tax_rates",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "subscription_credit_notes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    credit_note_number = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: false),
                    subscription_invoice_id = table.Column<Guid>(type: "uuid", nullable: false),
                    total_credit_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_subscription_credit_notes", x => x.id);
                    table.CheckConstraint("ck_subscription_credit_notes_total_credit_amount", "total_credit_amount >= 0");
                    table.ForeignKey(
                        name: "fk_subscription_credit_notes_subscription_invoice_id_subscription_invoices",
                        column: x => x.subscription_invoice_id,
                        principalTable: "subscription_invoices",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "subscription_invoice_lines",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    line_number = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: false),
                    line_total_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    quantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    subscription_invoice_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_subscription_invoice_lines", x => x.id);
                    table.CheckConstraint("ck_subscription_invoice_lines_line_total_amount", "line_total_amount >= 0");
                    table.CheckConstraint("ck_subscription_invoice_lines_quantity", "quantity > 0");
                    table.ForeignKey(
                        name: "fk_subscription_invoice_lines_subscription_invoice_id_subscription_invoices",
                        column: x => x.subscription_invoice_id,
                        principalTable: "subscription_invoices",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "subscription_payment_links",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    payment_link_token_hash = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false),
                    subscription_invoice_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_subscription_payment_links", x => x.id);
                    table.CheckConstraint("ck_subscription_payment_links_expires_at_created_at", "expires_at IS NULL OR expires_at > created_at");
                    table.ForeignKey(
                        name: "fk_subscription_payment_links_subscription_invoice_id_subscription_invoices",
                        column: x => x.subscription_invoice_id,
                        principalTable: "subscription_invoices",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "tenant_refresh_tokens",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false),
                    tenant_auth_session_id = table.Column<Guid>(type: "uuid", nullable: false),
                    token_hash = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tenant_refresh_tokens", x => x.id);
                    table.CheckConstraint("ck_tenant_refresh_tokens_status", "status IN ('ACTIVE', 'USED', 'EXPIRED', 'REVOKED')");
                    table.ForeignKey(
                        name: "fk_tenant_refresh_tokens_tenant_auth_session_id_tenant_auth_sessions",
                        column: x => x.tenant_auth_session_id,
                        principalTable: "tenant_auth_sessions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "notification_delivery_attempts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    attempt_number = table.Column<int>(type: "integer", nullable: false),
                    notification_channel_id = table.Column<Guid>(type: "uuid", nullable: false),
                    notification_message_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_notification_delivery_attempts", x => x.id);
                    table.CheckConstraint("ck_notification_delivery_attempts_attempt_number", "attempt_number > 0");
                    table.ForeignKey(
                        name: "fk_notification_delivery_attempts_notification_channel_id_notification_channels",
                        column: x => x.notification_channel_id,
                        principalTable: "notification_channels",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_notification_delivery_attempts_notification_message_id_notification_messages",
                        column: x => x.notification_message_id,
                        principalTable: "notification_messages",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "notification_inbox_items",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    inbox_status = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false),
                    notification_message_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_notification_inbox_items", x => x.id);
                    table.CheckConstraint("ck_notification_inbox_items_inbox_status", "inbox_status IN ('UNREAD', 'READ', 'ARCHIVED', 'DELETED')");
                    table.ForeignKey(
                        name: "fk_notification_inbox_items_notification_message_id_notification_messages",
                        column: x => x.notification_message_id,
                        principalTable: "notification_messages",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "customer_refresh_tokens",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    customer_auth_session_id = table.Column<Guid>(type: "uuid", nullable: false),
                    token_hash = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_customer_refresh_tokens", x => x.id);
                    table.ForeignKey(
                        name: "fk_customer_refresh_tokens_customer_auth_session_id_customer_auth_sessions",
                        column: x => x.customer_auth_session_id,
                        principalTable: "customer_auth_sessions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "fulfillment_order_events",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    fulfillment_order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sequence_number = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_fulfillment_order_events", x => x.id);
                    table.CheckConstraint("ck_fulfillment_order_events_sequence_number", "sequence_number > 0");
                    table.ForeignKey(
                        name: "fk_fulfillment_order_events_fulfillment_order_id_fulfillment_orders",
                        column: x => x.fulfillment_order_id,
                        principalTable: "fulfillment_orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "fulfillment_order_lines",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    fulfillment_order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    requested_quantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    sales_order_line_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_fulfillment_order_lines", x => x.id);
                    table.CheckConstraint("ck_fulfillment_order_lines_requested_quantity", "requested_quantity > 0");
                    table.ForeignKey(
                        name: "fk_fulfillment_order_lines_fulfillment_order_id_fulfillment_orders",
                        column: x => x.fulfillment_order_id,
                        principalTable: "fulfillment_orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_fulfillment_order_lines_sales_order_line_id_sales_order_lines",
                        column: x => x.sales_order_line_id,
                        principalTable: "sales_order_lines",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "pickup_slot_reservations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false),
                    checkout_session_id = table.Column<Guid>(type: "uuid", nullable: false),
                    pickup_slot_id = table.Column<Guid>(type: "uuid", nullable: false),
                    reserved_capacity = table.Column<int>(type: "integer", nullable: false),
                    sales_order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_pickup_slot_reservations", x => x.id);
                    table.CheckConstraint("ck_pickup_slot_reservations_checkout_session_id_sales_order_id", "checkout_session_id IS NOT NULL OR sales_order_id IS NOT NULL");
                    table.CheckConstraint("ck_pickup_slot_reservations_reserved_capacity", "reserved_capacity > 0");
                    table.ForeignKey(
                        name: "fk_pickup_slot_reservations_pickup_slot_id_pickup_slots",
                        column: x => x.pickup_slot_id,
                        principalTable: "pickup_slots",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "inventory_reservation_allocations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    allocated_quantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    inventory_balance_id = table.Column<Guid>(type: "uuid", nullable: false),
                    inventory_reservation_line_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_inventory_reservation_allocations", x => x.id);
                    table.CheckConstraint("ck_inventory_reservation_allocations_allocated_quantity", "allocated_quantity > 0");
                    table.ForeignKey(
                        name: "fk_inventory_reservation_allocations_inventory_balance_id_inventory_balances",
                        column: x => x.inventory_balance_id,
                        principalTable: "inventory_balances",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_inventory_reservation_allocations_inventory_reservation_line_id_inventory_reservation_lines",
                        column: x => x.inventory_reservation_line_id,
                        principalTable: "inventory_reservation_lines",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "stock_transfer_lines",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    line_number = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    requested_quantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    stock_transfer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_stock_transfer_lines", x => x.id);
                    table.CheckConstraint("ck_stock_transfer_lines_requested_quantity", "requested_quantity > 0");
                    table.ForeignKey(
                        name: "fk_stock_transfer_lines_product_id_products",
                        column: x => x.product_id,
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_stock_transfer_lines_stock_transfer_id_stock_transfers",
                        column: x => x.stock_transfer_id,
                        principalTable: "stock_transfers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "stock_transfer_status_history",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    sequence_number = table.Column<int>(type: "integer", nullable: false),
                    stock_transfer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_stock_transfer_status_history", x => x.id);
                    table.CheckConstraint("ck_stock_transfer_status_history_sequence_number", "sequence_number > 0");
                    table.ForeignKey(
                        name: "fk_stock_transfer_status_history_stock_transfer_id_stock_transfers",
                        column: x => x.stock_transfer_id,
                        principalTable: "stock_transfers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "stocktake_lines",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_batch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_variant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    stocktake_session_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_stocktake_lines", x => x.id);
                    table.ForeignKey(
                        name: "fk_stocktake_lines_product_id_products",
                        column: x => x.product_id,
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_stocktake_lines_stocktake_session_id_stocktake_sessions",
                        column: x => x.stocktake_session_id,
                        principalTable: "stocktake_sessions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "device_sync_states",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    offline_client_id = table.Column<Guid>(type: "uuid", nullable: false),
                    dataset_name = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: false),
                    last_client_version = table.Column<int>(type: "integer", nullable: false),
                    last_server_version = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_device_sync_states", x => x.id);
                    table.CheckConstraint("ck_device_sync_states_last_client_version", "last_client_version IS NULL OR last_client_version >= 0");
                    table.CheckConstraint("ck_device_sync_states_last_server_version", "last_server_version IS NULL OR last_server_version >= 0");
                    table.ForeignKey(
                        name: "fk_device_sync_states_offline_client_id_offline_clients",
                        column: x => x.offline_client_id,
                        principalTable: "offline_clients",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "offline_number_blocks",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    offline_client_id = table.Column<Guid>(type: "uuid", nullable: false),
                    document_number_sequence_id = table.Column<Guid>(type: "uuid", nullable: false),
                    next_value = table.Column<int>(type: "integer", nullable: false),
                    padding_length_snapshot = table.Column<int>(type: "integer", nullable: false),
                    range_end = table.Column<int>(type: "integer", nullable: false),
                    range_start = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_offline_number_blocks", x => x.id);
                    table.CheckConstraint("ck_offline_number_blocks_next_value_range_end", "next_value <= range_end + 1");
                    table.CheckConstraint("ck_offline_number_blocks_next_value_range_start", "next_value >= range_start");
                    table.CheckConstraint("ck_offline_number_blocks_padding_length_snapshot", "padding_length_snapshot > 0");
                    table.CheckConstraint("ck_offline_number_blocks_range_end_range_start", "range_end >= range_start");
                    table.CheckConstraint("ck_offline_number_blocks_range_start", "range_start > 0");
                    table.ForeignKey(
                        name: "fk_offline_number_blocks_document_number_sequence_id_document_number_sequences",
                        column: x => x.document_number_sequence_id,
                        principalTable: "document_number_sequences",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_offline_number_blocks_offline_client_id_offline_clients",
                        column: x => x.offline_client_id,
                        principalTable: "offline_clients",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "sync_batches",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    offline_client_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false),
                    conflict_count = table.Column<int>(type: "integer", nullable: false),
                    downloaded_item_count = table.Column<int>(type: "integer", nullable: false),
                    idempotency_key = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: false),
                    uploaded_item_count = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sync_batches", x => x.id);
                    table.CheckConstraint("ck_sync_batches_conflict_count", "conflict_count >= 0");
                    table.CheckConstraint("ck_sync_batches_downloaded_item_count", "downloaded_item_count >= 0");
                    table.CheckConstraint("ck_sync_batches_uploaded_item_count", "uploaded_item_count >= 0");
                    table.ForeignKey(
                        name: "fk_sync_batches_offline_client_id_offline_clients",
                        column: x => x.offline_client_id,
                        principalTable: "offline_clients",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "cash_movements",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    cash_movement_type_id = table.Column<Guid>(type: "uuid", nullable: false),
                    till_session_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_cash_movements", x => x.id);
                    table.CheckConstraint("ck_cash_movements_amount", "amount > 0");
                    table.ForeignKey(
                        name: "fk_cash_movements_cash_movement_type_id_cash_movement_types",
                        column: x => x.cash_movement_type_id,
                        principalTable: "cash_movement_types",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_cash_movements_till_session_id_till_sessions",
                        column: x => x.till_session_id,
                        principalTable: "till_sessions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "cash_reconciliations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    counted_cash_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    expected_cash_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    till_session_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_cash_reconciliations", x => x.id);
                    table.CheckConstraint("ck_cash_reconciliations_counted_cash_amount", "counted_cash_amount >= 0");
                    table.CheckConstraint("ck_cash_reconciliations_expected_cash_amount", "expected_cash_amount >= 0");
                    table.ForeignKey(
                        name: "fk_cash_reconciliations_till_session_id_till_sessions",
                        column: x => x.till_session_id,
                        principalTable: "till_sessions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "till_cash_movements",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    till_session_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_till_cash_movements", x => x.id);
                    table.CheckConstraint("ck_till_cash_movements_amount", "amount > 0");
                    table.ForeignKey(
                        name: "fk_till_cash_movements_till_session_id_till_sessions",
                        column: x => x.till_session_id,
                        principalTable: "till_sessions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "till_session_events",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    till_session_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_till_session_events", x => x.id);
                    table.CheckConstraint("ck_till_session_events_amount", "amount IS NULL OR amount >= 0");
                    table.ForeignKey(
                        name: "fk_till_session_events_till_session_id_till_sessions",
                        column: x => x.till_session_id,
                        principalTable: "till_sessions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "till_session_summaries",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    order_count = table.Column<int>(type: "integer", nullable: false),
                    refund_count = table.Column<int>(type: "integer", nullable: false),
                    till_session_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_till_session_summaries", x => x.id);
                    table.CheckConstraint("ck_till_session_summaries_order_count", "order_count >= 0");
                    table.CheckConstraint("ck_till_session_summaries_refund_count", "refund_count >= 0");
                    table.ForeignKey(
                        name: "fk_till_session_summaries_till_session_id_till_sessions",
                        column: x => x.till_session_id,
                        principalTable: "till_sessions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "combo_group_items",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    combo_group_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_variant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_combo_group_items", x => x.id);
                    table.CheckConstraint("ck_combo_group_items_sort_order", "sort_order >= 0");
                    table.ForeignKey(
                        name: "fk_combo_group_items_combo_group_id_combo_groups",
                        column: x => x.combo_group_id,
                        principalTable: "combo_groups",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_combo_group_items_product_id_products",
                        column: x => x.product_id,
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "choice_option_inventory_impacts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    ingredient_product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_choice_option_id = table.Column<Guid>(type: "uuid", nullable: false),
                    quantity_delta = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_choice_option_inventory_impacts", x => x.id);
                    table.CheckConstraint("ck_choice_option_inventory_impacts_quantity_delta", "quantity_delta <> 0");
                    table.ForeignKey(
                        name: "fk_choice_option_inventory_impacts_ingredient_product_id_products",
                        column: x => x.ingredient_product_id,
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_choice_option_inventory_impacts_product_choice_option_id_product_choice_options",
                        column: x => x.product_choice_option_id,
                        principalTable: "product_choice_options",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "product_variant_option_values",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_option_value_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_variant_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_product_variant_option_values", x => x.id);
                    table.ForeignKey(
                        name: "fk_product_variant_option_values_product_option_value_id_product_option_values",
                        column: x => x.product_option_value_id,
                        principalTable: "product_option_values",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_product_variant_option_values_product_variant_id_product_variants",
                        column: x => x.product_variant_id,
                        principalTable: "product_variants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "expiry_discount_applications",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    expiry_discount_rule_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_batch_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_expiry_discount_applications", x => x.id);
                    table.ForeignKey(
                        name: "fk_expiry_discount_applications_expiry_discount_rule_id_expiry_discount_rules",
                        column: x => x.expiry_discount_rule_id,
                        principalTable: "expiry_discount_rules",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_expiry_discount_applications_product_batch_id_product_batches",
                        column: x => x.product_batch_id,
                        principalTable: "product_batches",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "inventory_cost_layers",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_batch_id = table.Column<Guid>(type: "uuid", nullable: true),
                    quantity_remaining = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    unit_cost = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_inventory_cost_layers", x => x.id);
                    table.CheckConstraint("ck_inventory_cost_layers_quantity_remaining", "quantity_remaining >= 0");
                    table.CheckConstraint("ck_inventory_cost_layers_unit_cost", "unit_cost >= 0");
                    table.ForeignKey(
                        name: "fk_inventory_cost_layers_product_batch_id_product_batches",
                        column: x => x.product_batch_id,
                        principalTable: "product_batches",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "sales_exchange_events",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    sales_exchange_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sequence_number = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sales_exchange_events", x => x.id);
                    table.CheckConstraint("ck_sales_exchange_events_sequence_number", "sequence_number > 0");
                    table.ForeignKey(
                        name: "fk_sales_exchange_events_sales_exchange_id_sales_exchanges",
                        column: x => x.sales_exchange_id,
                        principalTable: "sales_exchanges",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "sales_refund_payment_allocations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    allocated_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    original_sales_payment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sales_refund_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sales_refund_payment_allocations", x => x.id);
                    table.CheckConstraint("ck_sales_refund_payment_allocations_allocated_amount", "allocated_amount > 0");
                    table.ForeignKey(
                        name: "fk_sales_refund_payment_allocations_original_sales_payment_id_sales_payments",
                        column: x => x.original_sales_payment_id,
                        principalTable: "sales_payments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_sales_refund_payment_allocations_sales_refund_id_sales_refunds",
                        column: x => x.sales_refund_id,
                        principalTable: "sales_refunds",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "return_inspections",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    inspected_quantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    inspection_number = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: false),
                    sales_return_line_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_return_inspections", x => x.id);
                    table.CheckConstraint("ck_return_inspections_inspected_quantity", "inspected_quantity >= 0");
                    table.ForeignKey(
                        name: "fk_return_inspections_sales_return_line_id_sales_return_lines",
                        column: x => x.sales_return_line_id,
                        principalTable: "sales_return_lines",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "sales_exchange_lines",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    replacement_quantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    returned_quantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    sales_exchange_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sales_return_line_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sales_exchange_lines", x => x.id);
                    table.CheckConstraint("ck_sales_exchange_lines_replacement_quantity", "replacement_quantity >= 0");
                    table.CheckConstraint("ck_sales_exchange_lines_returned_quantity", "returned_quantity > 0");
                    table.ForeignKey(
                        name: "fk_sales_exchange_lines_sales_exchange_id_sales_exchanges",
                        column: x => x.sales_exchange_id,
                        principalTable: "sales_exchanges",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_sales_exchange_lines_sales_return_line_id_sales_return_lines",
                        column: x => x.sales_return_line_id,
                        principalTable: "sales_return_lines",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "sales_refund_lines",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    sales_refund_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sales_return_line_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sales_refund_lines", x => x.id);
                    table.CheckConstraint("ck_sales_refund_lines_amount", "amount >= 0");
                    table.ForeignKey(
                        name: "fk_sales_refund_lines_sales_refund_id_sales_refunds",
                        column: x => x.sales_refund_id,
                        principalTable: "sales_refunds",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_sales_refund_lines_sales_return_line_id_sales_return_lines",
                        column: x => x.sales_return_line_id,
                        principalTable: "sales_return_lines",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "checkout_session_line_components",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    checkout_session_line_id = table.Column<Guid>(type: "uuid", nullable: false),
                    quantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_checkout_session_line_components", x => x.id);
                    table.CheckConstraint("ck_checkout_session_line_components_quantity", "quantity > 0");
                    table.ForeignKey(
                        name: "fk_checkout_session_line_components_checkout_session_line_id_checkout_session_lines",
                        column: x => x.checkout_session_line_id,
                        principalTable: "checkout_session_lines",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "checkout_session_line_options",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    checkout_session_line_id = table.Column<Guid>(type: "uuid", nullable: false),
                    quantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_checkout_session_line_options", x => x.id);
                    table.CheckConstraint("ck_checkout_session_line_options_quantity", "quantity > 0");
                    table.ForeignKey(
                        name: "fk_checkout_session_line_options_checkout_session_line_id_checkout_session_lines",
                        column: x => x.checkout_session_line_id,
                        principalTable: "checkout_session_lines",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "subscription_credit_note_lines",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    line_credit_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    line_number = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: false),
                    subscription_credit_note_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_subscription_credit_note_lines", x => x.id);
                    table.CheckConstraint("ck_subscription_credit_note_lines_line_credit_amount", "line_credit_amount >= 0");
                    table.ForeignKey(
                        name: "fk_subscription_credit_note_lines_subscription_credit_note_id_subscription_credit_notes",
                        column: x => x.subscription_credit_note_id,
                        principalTable: "subscription_credit_notes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "subscription_payment_transactions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    provider_transaction_reference = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false),
                    subscription_invoice_id = table.Column<Guid>(type: "uuid", nullable: false),
                    subscription_payment_link_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_subscription_payment_transactions", x => x.id);
                    table.CheckConstraint("ck_subscription_payment_transactions_amount", "amount >= 0");
                    table.ForeignKey(
                        name: "fk_subscription_payment_transactions_subscription_invoice_id_subscription_invoices",
                        column: x => x.subscription_invoice_id,
                        principalTable: "subscription_invoices",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_subscription_payment_transactions_subscription_payment_link_id_subscription_payment_links",
                        column: x => x.subscription_payment_link_id,
                        principalTable: "subscription_payment_links",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "notification_read_receipts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    notification_inbox_item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    read_source = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_notification_read_receipts", x => x.id);
                    table.CheckConstraint("ck_notification_read_receipts_read_source", "read_source IN ('WEB', 'MOBILE', 'POS', 'ADMIN', 'API')");
                    table.ForeignKey(
                        name: "fk_notification_read_receipts_notification_inbox_item_id_notification_inbox_items",
                        column: x => x.notification_inbox_item_id,
                        principalTable: "notification_inbox_items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "pickup_orders",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false),
                    fulfillment_order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    pickup_number = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: false),
                    pickup_slot_reservation_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_pickup_orders", x => x.id);
                    table.ForeignKey(
                        name: "fk_pickup_orders_fulfillment_order_id_fulfillment_orders",
                        column: x => x.fulfillment_order_id,
                        principalTable: "fulfillment_orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_pickup_orders_pickup_slot_reservation_id_pickup_slot_reservations",
                        column: x => x.pickup_slot_reservation_id,
                        principalTable: "pickup_slot_reservations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "stocktake_line_serials",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    serial_number_id = table.Column<Guid>(type: "uuid", nullable: false),
                    stocktake_line_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_stocktake_line_serials", x => x.id);
                    table.ForeignKey(
                        name: "fk_stocktake_line_serials_serial_number_id_serial_numbers",
                        column: x => x.serial_number_id,
                        principalTable: "serial_numbers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_stocktake_line_serials_stocktake_line_id_stocktake_lines",
                        column: x => x.stocktake_line_id,
                        principalTable: "stocktake_lines",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "sync_items",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    offline_client_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false),
                    operation_type = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: false),
                    client_record_id = table.Column<Guid>(type: "uuid", nullable: false),
                    entity_name = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: false),
                    payload_hash = table.Column<string>(type: "char(64)", maxLength: 64, nullable: false),
                    sync_batch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sync_items", x => x.id);
                    table.ForeignKey(
                        name: "fk_sync_items_offline_client_id_offline_clients",
                        column: x => x.offline_client_id,
                        principalTable: "offline_clients",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_sync_items_sync_batch_id_sync_batches",
                        column: x => x.sync_batch_id,
                        principalTable: "sync_batches",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "cash_count_denominations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    cash_reconciliation_id = table.Column<Guid>(type: "uuid", nullable: false),
                    denomination_value = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    quantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_cash_count_denominations", x => x.id);
                    table.CheckConstraint("ck_cash_count_denominations_denomination_value", "denomination_value > 0");
                    table.CheckConstraint("ck_cash_count_denominations_quantity", "quantity >= 0");
                    table.ForeignKey(
                        name: "fk_cash_count_denominations_cash_reconciliation_id_cash_reconciliations",
                        column: x => x.cash_reconciliation_id,
                        principalTable: "cash_reconciliations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "till_session_payment_summaries",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    payment_method_id = table.Column<Guid>(type: "uuid", nullable: false),
                    till_session_summary_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_till_session_payment_summaries", x => x.id);
                    table.ForeignKey(
                        name: "fk_till_session_payment_summaries_payment_method_id_payment_methods",
                        column: x => x.payment_method_id,
                        principalTable: "payment_methods",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_till_session_payment_summaries_till_session_summary_id_till_session_summaries",
                        column: x => x.till_session_summary_id,
                        principalTable: "till_session_summaries",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "stock_movement_cost_allocations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    allocated_cost_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    allocated_quantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    inventory_cost_layer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    stock_movement_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_stock_movement_cost_allocations", x => x.id);
                    table.CheckConstraint("ck_stock_movement_cost_allocations_allocated_cost_amount", "allocated_cost_amount >= 0");
                    table.CheckConstraint("ck_stock_movement_cost_allocations_allocated_quantity", "allocated_quantity > 0");
                    table.ForeignKey(
                        name: "fk_stock_movement_cost_allocations_inventory_cost_layer_id_inventory_cost_layers",
                        column: x => x.inventory_cost_layer_id,
                        principalTable: "inventory_cost_layers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_stock_movement_cost_allocations_stock_movement_id_stock_movements",
                        column: x => x.stock_movement_id,
                        principalTable: "stock_movements",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "pickup_order_events",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    pickup_order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sequence_number = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_pickup_order_events", x => x.id);
                    table.CheckConstraint("ck_pickup_order_events_sequence_number", "sequence_number > 0");
                    table.ForeignKey(
                        name: "fk_pickup_order_events_pickup_order_id_pickup_orders",
                        column: x => x.pickup_order_id,
                        principalTable: "pickup_orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "offline_id_mappings",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    offline_client_id = table.Column<Guid>(type: "uuid", nullable: false),
                    client_record_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_from_sync_item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    entity_name = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: false),
                    server_record_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_offline_id_mappings", x => x.id);
                    table.ForeignKey(
                        name: "fk_offline_id_mappings_created_from_sync_item_id_sync_items",
                        column: x => x.created_from_sync_item_id,
                        principalTable: "sync_items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_offline_id_mappings_offline_client_id_offline_clients",
                        column: x => x.offline_client_id,
                        principalTable: "offline_clients",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "sync_conflicts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    offline_client_id = table.Column<Guid>(type: "uuid", nullable: false),
                    resolution_status = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false),
                    sync_batch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sync_item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sync_conflicts", x => x.id);
                    table.CheckConstraint("ck_sync_conflicts_resolution_status", "resolution_status IN ('OPEN', 'RESOLVED', 'IGNORED', 'FAILED')");
                    table.ForeignKey(
                        name: "fk_sync_conflicts_offline_client_id_offline_clients",
                        column: x => x.offline_client_id,
                        principalTable: "offline_clients",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_sync_conflicts_sync_batch_id_sync_batches",
                        column: x => x.sync_batch_id,
                        principalTable: "sync_batches",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_sync_conflicts_sync_item_id_sync_items",
                        column: x => x.sync_item_id,
                        principalTable: "sync_items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "uq_brands_tenant_id_brand_code",
                table: "brands",
                columns: new[] { "tenant_id", "brand_code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_business_type_option_templates_product_option_template_id",
                table: "business_type_option_templates",
                column: "product_option_template_id");

            migrationBuilder.CreateIndex(
                name: "uq_business_type_option_templates_business_type_id_product_option_template_id",
                table: "business_type_option_templates",
                columns: new[] { "business_type_id", "product_option_template_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_business_types_business_type_code",
                table: "business_types",
                column: "business_type_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_cash_count_denominations_cash_reconciliation_id_denomination_value",
                table: "cash_count_denominations",
                columns: new[] { "cash_reconciliation_id", "denomination_value" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_cash_movement_types_tenant_id_movement_type_code",
                table: "cash_movement_types",
                columns: new[] { "tenant_id", "movement_type_code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_cash_movements_cash_movement_type_id",
                table: "cash_movements",
                column: "cash_movement_type_id");

            migrationBuilder.CreateIndex(
                name: "IX_cash_movements_till_session_id",
                table: "cash_movements",
                column: "till_session_id");

            migrationBuilder.CreateIndex(
                name: "uq_cash_reconciliations_till_session_id",
                table: "cash_reconciliations",
                column: "till_session_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_categories_parent_category_id",
                table: "categories",
                column: "parent_category_id");

            migrationBuilder.CreateIndex(
                name: "uq_categories_tenant_id_category_code",
                table: "categories",
                columns: new[] { "tenant_id", "category_code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_checkout_events_checkout_session_id_sequence_number",
                table: "checkout_events",
                columns: new[] { "checkout_session_id", "sequence_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_checkout_session_line_components_checkout_session_line_id",
                table: "checkout_session_line_components",
                column: "checkout_session_line_id");

            migrationBuilder.CreateIndex(
                name: "IX_checkout_session_line_options_checkout_session_line_id",
                table: "checkout_session_line_options",
                column: "checkout_session_line_id");

            migrationBuilder.CreateIndex(
                name: "uq_checkout_session_lines_checkout_session_id_line_number",
                table: "checkout_session_lines",
                columns: new[] { "checkout_session_id", "line_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_checkout_sessions_shopping_cart_id",
                table: "checkout_sessions",
                column: "shopping_cart_id");

            migrationBuilder.CreateIndex(
                name: "uq_checkout_sessions_tenant_id_checkout_number",
                table: "checkout_sessions",
                columns: new[] { "tenant_id", "checkout_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_choice_groups_tenant_id_choice_group_code",
                table: "choice_groups",
                columns: new[] { "tenant_id", "choice_group_code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_choice_option_inventory_impacts_ingredient_product_id",
                table: "choice_option_inventory_impacts",
                column: "ingredient_product_id");

            migrationBuilder.CreateIndex(
                name: "IX_choice_option_inventory_impacts_product_choice_option_id",
                table: "choice_option_inventory_impacts",
                column: "product_choice_option_id");

            migrationBuilder.CreateIndex(
                name: "uq_choice_options_choice_group_id_option_code",
                table: "choice_options",
                columns: new[] { "choice_group_id", "option_code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_collections_tenant_id_collection_code",
                table: "collections",
                columns: new[] { "tenant_id", "collection_code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_combo_components_component_product_id",
                table: "combo_components",
                column: "component_product_id");

            migrationBuilder.CreateIndex(
                name: "uq_combo_components_combo_definition_id_component_product_id_component_variant_id",
                table: "combo_components",
                columns: new[] { "combo_definition_id", "component_product_id", "component_variant_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_combo_definitions_product_id",
                table: "combo_definitions",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "uq_combo_definitions_tenant_id_product_id_combo_code",
                table: "combo_definitions",
                columns: new[] { "tenant_id", "product_id", "combo_code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_combo_group_items_product_id",
                table: "combo_group_items",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "uq_combo_group_items_combo_group_id_product_id_product_variant_id",
                table: "combo_group_items",
                columns: new[] { "combo_group_id", "product_id", "product_variant_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_combo_groups_combo_definition_id_group_code",
                table: "combo_groups",
                columns: new[] { "combo_definition_id", "group_code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_currencies_currency_code",
                table: "currencies",
                column: "currency_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_customer_auth_accounts_customer_id",
                table: "customer_auth_accounts",
                column: "customer_id");

            migrationBuilder.CreateIndex(
                name: "uq_customer_auth_accounts_tenant_id_customer_id",
                table: "customer_auth_accounts",
                columns: new[] { "tenant_id", "customer_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_customer_auth_sessions_customer_auth_account_id",
                table: "customer_auth_sessions",
                column: "customer_auth_account_id");

            migrationBuilder.CreateIndex(
                name: "uq_customer_auth_sessions_tenant_id_session_token_hash",
                table: "customer_auth_sessions",
                columns: new[] { "tenant_id", "session_token_hash" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_customer_consents_customer_id",
                table: "customer_consents",
                column: "customer_id");

            migrationBuilder.CreateIndex(
                name: "uq_customer_consents_tenant_id_customer_id_consent_type_sales_channel_id",
                table: "customer_consents",
                columns: new[] { "tenant_id", "customer_id", "consent_type", "sales_channel_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_customer_password_reset_tokens_customer_auth_account_id",
                table: "customer_password_reset_tokens",
                column: "customer_auth_account_id");

            migrationBuilder.CreateIndex(
                name: "IX_customer_password_reset_tokens_verified_otp_id",
                table: "customer_password_reset_tokens",
                column: "verified_otp_id");

            migrationBuilder.CreateIndex(
                name: "uq_customer_password_reset_tokens_tenant_id_token_hash",
                table: "customer_password_reset_tokens",
                columns: new[] { "tenant_id", "token_hash" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_customer_refresh_tokens_customer_auth_session_id",
                table: "customer_refresh_tokens",
                column: "customer_auth_session_id");

            migrationBuilder.CreateIndex(
                name: "uq_customer_refresh_tokens_tenant_id_id",
                table: "customer_refresh_tokens",
                columns: new[] { "tenant_id", "id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_customer_refresh_tokens_tenant_id_token_hash",
                table: "customer_refresh_tokens",
                columns: new[] { "tenant_id", "token_hash" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_customer_verification_otps_customer_id",
                table: "customer_verification_otps",
                column: "customer_id");

            migrationBuilder.CreateIndex(
                name: "uq_customer_verification_otps_tenant_id_verification_purpose_normalized_recipient_value",
                table: "customer_verification_otps",
                columns: new[] { "tenant_id", "verification_purpose", "normalized_recipient_value" },
                unique: true,
                filter: "status = 'PENDING'");

            migrationBuilder.CreateIndex(
                name: "uq_customers_tenant_id_customer_code",
                table: "customers",
                columns: new[] { "tenant_id", "customer_code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_customers_tenant_id_normalized_email",
                table: "customers",
                columns: new[] { "tenant_id", "normalized_email" },
                unique: true,
                filter: "normalized_email IS NOT NULL AND status <> 'DELETED'");

            migrationBuilder.CreateIndex(
                name: "uq_customers_tenant_id_normalized_phone",
                table: "customers",
                columns: new[] { "tenant_id", "normalized_phone" },
                unique: true,
                filter: "normalized_phone IS NOT NULL AND status <> 'DELETED'");

            migrationBuilder.CreateIndex(
                name: "uq_departments_tenant_id_department_code",
                table: "departments",
                columns: new[] { "tenant_id", "department_code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_device_sync_states_offline_client_id",
                table: "device_sync_states",
                column: "offline_client_id");

            migrationBuilder.CreateIndex(
                name: "uq_device_sync_states_tenant_id_offline_client_id_dataset_name",
                table: "device_sync_states",
                columns: new[] { "tenant_id", "offline_client_id", "dataset_name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_discount_policies_discount_type_id",
                table: "discount_policies",
                column: "discount_type_id");

            migrationBuilder.CreateIndex(
                name: "uq_discount_policies_tenant_id_discount_code",
                table: "discount_policies",
                columns: new[] { "tenant_id", "discount_code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_discount_policy_channels_sales_channel_id",
                table: "discount_policy_channels",
                column: "sales_channel_id");

            migrationBuilder.CreateIndex(
                name: "uq_discount_policy_channels_discount_policy_id_sales_channel_id",
                table: "discount_policy_channels",
                columns: new[] { "discount_policy_id", "sales_channel_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_discount_policy_conditions_discount_policy_id",
                table: "discount_policy_conditions",
                column: "discount_policy_id");

            migrationBuilder.CreateIndex(
                name: "IX_discount_policy_outlets_outlet_id",
                table: "discount_policy_outlets",
                column: "outlet_id");

            migrationBuilder.CreateIndex(
                name: "uq_discount_policy_outlets_discount_policy_id_outlet_id",
                table: "discount_policy_outlets",
                columns: new[] { "discount_policy_id", "outlet_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_discount_policy_targets_discount_policy_id_target_type_target_id",
                table: "discount_policy_targets",
                columns: new[] { "discount_policy_id", "target_type", "target_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_discount_types_tenant_id_discount_type_code",
                table: "discount_types",
                columns: new[] { "tenant_id", "discount_type_code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_document_number_sequences_tenant_id_document_type_document_subtype_sales_channel_id_outlet_id",
                table: "document_number_sequences",
                columns: new[] { "tenant_id", "document_type", "document_subtype", "sales_channel_id", "outlet_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_email_verification_tokens_tenant_user_id",
                table: "email_verification_tokens",
                column: "tenant_user_id");

            migrationBuilder.CreateIndex(
                name: "uq_email_verification_tokens_token_hash",
                table: "email_verification_tokens",
                column: "token_hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_expiry_discount_applications_expiry_discount_rule_id",
                table: "expiry_discount_applications",
                column: "expiry_discount_rule_id");

            migrationBuilder.CreateIndex(
                name: "uq_expiry_discount_applications_product_batch_id_expiry_discount_rule_id",
                table: "expiry_discount_applications",
                columns: new[] { "product_batch_id", "expiry_discount_rule_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_expiry_discount_rule_tiers_expiry_discount_rule_id",
                table: "expiry_discount_rule_tiers",
                column: "expiry_discount_rule_id");

            migrationBuilder.CreateIndex(
                name: "uq_expiry_discount_rules_tenant_id_rule_code",
                table: "expiry_discount_rules",
                columns: new[] { "tenant_id", "rule_code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_feature_flags_platform_feature_id",
                table: "feature_flags",
                column: "platform_feature_id");

            migrationBuilder.CreateIndex(
                name: "uq_feature_flags_flag_code",
                table: "feature_flags",
                column: "flag_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_feature_limit_definitions_platform_feature_id_limit_code",
                table: "feature_limit_definitions",
                columns: new[] { "platform_feature_id", "limit_code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_fulfillment_method_outlets_outlet_id",
                table: "fulfillment_method_outlets",
                column: "outlet_id");

            migrationBuilder.CreateIndex(
                name: "uq_fulfillment_method_outlets_fulfillment_method_id_outlet_id",
                table: "fulfillment_method_outlets",
                columns: new[] { "fulfillment_method_id", "outlet_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_fulfillment_methods_tenant_id_method_code",
                table: "fulfillment_methods",
                columns: new[] { "tenant_id", "method_code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_fulfillment_order_events_fulfillment_order_id_sequence_number",
                table: "fulfillment_order_events",
                columns: new[] { "fulfillment_order_id", "sequence_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_fulfillment_order_lines_fulfillment_order_id",
                table: "fulfillment_order_lines",
                column: "fulfillment_order_id");

            migrationBuilder.CreateIndex(
                name: "IX_fulfillment_order_lines_sales_order_line_id",
                table: "fulfillment_order_lines",
                column: "sales_order_line_id");

            migrationBuilder.CreateIndex(
                name: "IX_fulfillment_orders_fulfillment_method_outlet_id",
                table: "fulfillment_orders",
                column: "fulfillment_method_outlet_id");

            migrationBuilder.CreateIndex(
                name: "IX_fulfillment_orders_sales_order_id",
                table: "fulfillment_orders",
                column: "sales_order_id");

            migrationBuilder.CreateIndex(
                name: "uq_fulfillment_orders_tenant_id_fulfillment_number",
                table: "fulfillment_orders",
                columns: new[] { "tenant_id", "fulfillment_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_hardware_device_assignments_pos_device_id",
                table: "hardware_device_assignments",
                column: "pos_device_id");

            migrationBuilder.CreateIndex(
                name: "uq_hardware_device_assignments_hardware_device_id_pos_device_id_effective_from",
                table: "hardware_device_assignments",
                columns: new[] { "hardware_device_id", "pos_device_id", "effective_from" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_hardware_devices_outlet_id",
                table: "hardware_devices",
                column: "outlet_id");

            migrationBuilder.CreateIndex(
                name: "uq_hardware_devices_serial_number",
                table: "hardware_devices",
                column: "serial_number",
                unique: true,
                filter: "serial_number IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "uq_hardware_devices_tenant_id_hardware_device_code",
                table: "hardware_devices",
                columns: new[] { "tenant_id", "hardware_device_code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_hardware_profiles_tenant_id_profile_code",
                table: "hardware_profiles",
                columns: new[] { "tenant_id", "profile_code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_hardware_test_logs_hardware_device_id",
                table: "hardware_test_logs",
                column: "hardware_device_id");

            migrationBuilder.CreateIndex(
                name: "uq_integration_providers_provider_code",
                table: "integration_providers",
                column: "provider_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_inventory_balances_product_id",
                table: "inventory_balances",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "uq_inventory_balances_inventory_location_id_product_id_product_variant_id_product_batch_id",
                table: "inventory_balances",
                columns: new[] { "inventory_location_id", "product_id", "product_variant_id", "product_batch_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_inventory_channel_allocations_sales_channel_id",
                table: "inventory_channel_allocations",
                column: "sales_channel_id");

            migrationBuilder.CreateIndex(
                name: "uq_inventory_channel_allocations_inventory_location_id_product_id_product_variant_id_sales_channel_id",
                table: "inventory_channel_allocations",
                columns: new[] { "inventory_location_id", "product_id", "product_variant_id", "sales_channel_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_inventory_cost_layers_product_batch_id",
                table: "inventory_cost_layers",
                column: "product_batch_id");

            migrationBuilder.CreateIndex(
                name: "IX_inventory_locations_outlet_id",
                table: "inventory_locations",
                column: "outlet_id");

            migrationBuilder.CreateIndex(
                name: "uq_inventory_locations_tenant_id_location_code",
                table: "inventory_locations",
                columns: new[] { "tenant_id", "location_code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_inventory_reorder_rules_product_id",
                table: "inventory_reorder_rules",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "uq_inventory_reorder_rules_inventory_location_id_product_id_product_variant_id",
                table: "inventory_reorder_rules",
                columns: new[] { "inventory_location_id", "product_id", "product_variant_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_inventory_reservation_allocations_inventory_balance_id",
                table: "inventory_reservation_allocations",
                column: "inventory_balance_id");

            migrationBuilder.CreateIndex(
                name: "IX_inventory_reservation_allocations_inventory_reservation_lin~",
                table: "inventory_reservation_allocations",
                column: "inventory_reservation_line_id");

            migrationBuilder.CreateIndex(
                name: "IX_inventory_reservation_lines_product_id",
                table: "inventory_reservation_lines",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "uq_inventory_reservation_lines_inventory_reservation_id_line_number",
                table: "inventory_reservation_lines",
                columns: new[] { "inventory_reservation_id", "line_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_inventory_reservations_tenant_id_reservation_number",
                table: "inventory_reservations",
                columns: new[] { "tenant_id", "reservation_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_notification_channels_platform_integration_id",
                table: "notification_channels",
                column: "platform_integration_id");

            migrationBuilder.CreateIndex(
                name: "uq_notification_channels_tenant_id_channel_code",
                table: "notification_channels",
                columns: new[] { "tenant_id", "channel_code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_notification_delivery_attempts_notification_channel_id",
                table: "notification_delivery_attempts",
                column: "notification_channel_id");

            migrationBuilder.CreateIndex(
                name: "uq_notification_delivery_attempts_notification_message_id_attempt_number",
                table: "notification_delivery_attempts",
                columns: new[] { "notification_message_id", "attempt_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_notification_event_types_tenant_id_event_code",
                table: "notification_event_types",
                columns: new[] { "tenant_id", "event_code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_notification_events_notification_event_type_id",
                table: "notification_events",
                column: "notification_event_type_id");

            migrationBuilder.CreateIndex(
                name: "uq_notification_events_tenant_id_event_number",
                table: "notification_events",
                columns: new[] { "tenant_id", "event_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_notification_events_tenant_id_idempotency_key",
                table: "notification_events",
                columns: new[] { "tenant_id", "idempotency_key" },
                unique: true,
                filter: "idempotency_key IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "uq_notification_inbox_items_notification_message_id",
                table: "notification_inbox_items",
                column: "notification_message_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_notification_messages_notification_channel_id",
                table: "notification_messages",
                column: "notification_channel_id");

            migrationBuilder.CreateIndex(
                name: "IX_notification_messages_notification_event_id",
                table: "notification_messages",
                column: "notification_event_id");

            migrationBuilder.CreateIndex(
                name: "IX_notification_messages_notification_template_version_id",
                table: "notification_messages",
                column: "notification_template_version_id");

            migrationBuilder.CreateIndex(
                name: "uq_notification_messages_tenant_id_message_number",
                table: "notification_messages",
                columns: new[] { "tenant_id", "message_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_notification_preferences_notification_event_type_id",
                table: "notification_preferences",
                column: "notification_event_type_id");

            migrationBuilder.CreateIndex(
                name: "uq_notification_preferences_tenant_id_recipient_type_platform_user_id_tenant_user_id_customer_id_notification_event_type_id_channel_type",
                table: "notification_preferences",
                columns: new[] { "tenant_id", "recipient_type", "platform_user_id", "tenant_user_id", "customer_id", "notification_event_type_id", "channel_type" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_notification_read_receipts_notification_inbox_item_id",
                table: "notification_read_receipts",
                column: "notification_inbox_item_id");

            migrationBuilder.CreateIndex(
                name: "uq_notification_template_versions_notification_template_id",
                table: "notification_template_versions",
                column: "notification_template_id",
                unique: true,
                filter: "is_active_version = true");

            migrationBuilder.CreateIndex(
                name: "uq_notification_template_versions_notification_template_id_version_number",
                table: "notification_template_versions",
                columns: new[] { "notification_template_id", "version_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_notification_templates_notification_event_type_id",
                table: "notification_templates",
                column: "notification_event_type_id");

            migrationBuilder.CreateIndex(
                name: "uq_notification_templates_tenant_id_notification_event_type_id_channel_type_locale_template_code",
                table: "notification_templates",
                columns: new[] { "tenant_id", "notification_event_type_id", "channel_type", "locale", "template_code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_offline_clients_outlet_id",
                table: "offline_clients",
                column: "outlet_id");

            migrationBuilder.CreateIndex(
                name: "IX_offline_clients_pos_device_id",
                table: "offline_clients",
                column: "pos_device_id");

            migrationBuilder.CreateIndex(
                name: "uq_offline_clients_tenant_id_client_code",
                table: "offline_clients",
                columns: new[] { "tenant_id", "client_code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_offline_clients_tenant_id_pos_device_id",
                table: "offline_clients",
                columns: new[] { "tenant_id", "pos_device_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_offline_id_mappings_created_from_sync_item_id",
                table: "offline_id_mappings",
                column: "created_from_sync_item_id");

            migrationBuilder.CreateIndex(
                name: "IX_offline_id_mappings_offline_client_id",
                table: "offline_id_mappings",
                column: "offline_client_id");

            migrationBuilder.CreateIndex(
                name: "uq_offline_id_mappings_tenant_id_entity_name_server_record_id",
                table: "offline_id_mappings",
                columns: new[] { "tenant_id", "entity_name", "server_record_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_offline_id_mappings_tenant_id_offline_client_id_entity_name_client_record_id",
                table: "offline_id_mappings",
                columns: new[] { "tenant_id", "offline_client_id", "entity_name", "client_record_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_offline_number_blocks_document_number_sequence_id",
                table: "offline_number_blocks",
                column: "document_number_sequence_id");

            migrationBuilder.CreateIndex(
                name: "IX_offline_number_blocks_offline_client_id",
                table: "offline_number_blocks",
                column: "offline_client_id");

            migrationBuilder.CreateIndex(
                name: "IX_outlet_addresses_outlet_id",
                table: "outlet_addresses",
                column: "outlet_id");

            migrationBuilder.CreateIndex(
                name: "uq_outlet_business_hours_outlet_id_day_of_week",
                table: "outlet_business_hours",
                columns: new[] { "outlet_id", "day_of_week" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_outlet_user_permissions_permission_definition_id",
                table: "outlet_user_permissions",
                column: "permission_definition_id");

            migrationBuilder.CreateIndex(
                name: "IX_outlet_user_permissions_tenant_user_id",
                table: "outlet_user_permissions",
                column: "tenant_user_id");

            migrationBuilder.CreateIndex(
                name: "uq_outlet_user_permissions_outlet_id_tenant_user_id_permission_definition_id",
                table: "outlet_user_permissions",
                columns: new[] { "outlet_id", "tenant_user_id", "permission_definition_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_outlet_user_roles_tenant_role_id",
                table: "outlet_user_roles",
                column: "tenant_role_id");

            migrationBuilder.CreateIndex(
                name: "IX_outlet_user_roles_tenant_user_id",
                table: "outlet_user_roles",
                column: "tenant_user_id");

            migrationBuilder.CreateIndex(
                name: "uq_outlet_user_roles_outlet_id_tenant_user_id_tenant_role_id",
                table: "outlet_user_roles",
                columns: new[] { "outlet_id", "tenant_user_id", "tenant_role_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_outlets_tenant_id_outlet_code",
                table: "outlets",
                columns: new[] { "tenant_id", "outlet_code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_password_reset_tokens_tenant_user_id",
                table: "password_reset_tokens",
                column: "tenant_user_id");

            migrationBuilder.CreateIndex(
                name: "uq_password_reset_tokens_token_hash",
                table: "password_reset_tokens",
                column: "token_hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_payment_methods_tenant_id_method_code",
                table: "payment_methods",
                columns: new[] { "tenant_id", "method_code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_permission_definitions_permission_code",
                table: "permission_definitions",
                column: "permission_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_pickup_order_events_pickup_order_id_sequence_number",
                table: "pickup_order_events",
                columns: new[] { "pickup_order_id", "sequence_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_pickup_orders_fulfillment_order_id",
                table: "pickup_orders",
                column: "fulfillment_order_id");

            migrationBuilder.CreateIndex(
                name: "IX_pickup_orders_pickup_slot_reservation_id",
                table: "pickup_orders",
                column: "pickup_slot_reservation_id");

            migrationBuilder.CreateIndex(
                name: "uq_pickup_orders_tenant_id_pickup_number",
                table: "pickup_orders",
                columns: new[] { "tenant_id", "pickup_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_pickup_slot_reservations_pickup_slot_id",
                table: "pickup_slot_reservations",
                column: "pickup_slot_id");

            migrationBuilder.CreateIndex(
                name: "uq_pickup_slots_fulfillment_method_outlet_id_slot_date_window_start_window_end",
                table: "pickup_slots",
                columns: new[] { "fulfillment_method_outlet_id", "slot_date", "window_start", "window_end" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_platform_auth_sessions_platform_user_id",
                table: "platform_auth_sessions",
                column: "platform_user_id");

            migrationBuilder.CreateIndex(
                name: "uq_platform_auth_sessions_session_token_hash",
                table: "platform_auth_sessions",
                column: "session_token_hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_platform_features_platform_module_id_feature_code",
                table: "platform_features",
                columns: new[] { "platform_module_id", "feature_code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_platform_integration_credentials_platform_integration_id_credential_name",
                table: "platform_integration_credentials",
                columns: new[] { "platform_integration_id", "credential_name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_platform_integration_request_logs_integration_provider_id",
                table: "platform_integration_request_logs",
                column: "integration_provider_id");

            migrationBuilder.CreateIndex(
                name: "IX_platform_integration_request_logs_platform_integration_id",
                table: "platform_integration_request_logs",
                column: "platform_integration_id");

            migrationBuilder.CreateIndex(
                name: "IX_platform_integration_webhook_events_platform_integration_id",
                table: "platform_integration_webhook_events",
                column: "platform_integration_id");

            migrationBuilder.CreateIndex(
                name: "uq_platform_webhook_events_provider_external_event",
                table: "platform_integration_webhook_events",
                columns: new[] { "integration_provider_id", "external_event_id" },
                unique: true,
                filter: "external_event_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "uq_platform_webhook_events_provider_idempotency_key",
                table: "platform_integration_webhook_events",
                columns: new[] { "integration_provider_id", "idempotency_key" },
                unique: true,
                filter: "idempotency_key IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_platform_integrations_integration_provider_id",
                table: "platform_integrations",
                column: "integration_provider_id");

            migrationBuilder.CreateIndex(
                name: "uq_platform_integrations_integration_code",
                table: "platform_integrations",
                column: "integration_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_platform_login_audits_platform_user_id",
                table: "platform_login_audits",
                column: "platform_user_id");

            migrationBuilder.CreateIndex(
                name: "uq_platform_modules_module_code",
                table: "platform_modules",
                column: "module_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_platform_password_reset_tokens_platform_user_id",
                table: "platform_password_reset_tokens",
                column: "platform_user_id");

            migrationBuilder.CreateIndex(
                name: "uq_platform_password_reset_tokens_token_hash",
                table: "platform_password_reset_tokens",
                column: "token_hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_platform_permissions_permission_code",
                table: "platform_permissions",
                column: "permission_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_platform_refresh_tokens_platform_auth_session_id",
                table: "platform_refresh_tokens",
                column: "platform_auth_session_id");

            migrationBuilder.CreateIndex(
                name: "uq_platform_refresh_tokens_token_hash",
                table: "platform_refresh_tokens",
                column: "token_hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_platform_role_permissions_platform_permission_id",
                table: "platform_role_permissions",
                column: "platform_permission_id");

            migrationBuilder.CreateIndex(
                name: "uq_platform_role_permissions_platform_role_id_platform_permission_id",
                table: "platform_role_permissions",
                columns: new[] { "platform_role_id", "platform_permission_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_platform_roles_role_code",
                table: "platform_roles",
                column: "role_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_platform_user_permissions_platform_permission_id",
                table: "platform_user_permissions",
                column: "platform_permission_id");

            migrationBuilder.CreateIndex(
                name: "uq_platform_user_permissions_platform_user_id_platform_permission_id",
                table: "platform_user_permissions",
                columns: new[] { "platform_user_id", "platform_permission_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_platform_user_roles_platform_role_id",
                table: "platform_user_roles",
                column: "platform_role_id");

            migrationBuilder.CreateIndex(
                name: "uq_platform_user_roles_platform_user_id_platform_role_id",
                table: "platform_user_roles",
                columns: new[] { "platform_user_id", "platform_role_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_platform_users_email",
                table: "platform_users",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_platform_users_normalized_email",
                table: "platform_users",
                column: "normalized_email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_pos_devices_outlet_id",
                table: "pos_devices",
                column: "outlet_id");

            migrationBuilder.CreateIndex(
                name: "uq_pos_devices_device_serial_number",
                table: "pos_devices",
                column: "device_serial_number",
                unique: true,
                filter: "device_serial_number IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "uq_pos_devices_tenant_id_device_code",
                table: "pos_devices",
                columns: new[] { "tenant_id", "device_code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_pos_order_holds_sales_order_id",
                table: "pos_order_holds",
                column: "sales_order_id");

            migrationBuilder.CreateIndex(
                name: "uq_pos_order_holds_tenant_id_hold_number",
                table: "pos_order_holds",
                columns: new[] { "tenant_id", "hold_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_price_list_channels_sales_channel_id",
                table: "price_list_channels",
                column: "sales_channel_id");

            migrationBuilder.CreateIndex(
                name: "uq_price_list_channels_price_list_id_sales_channel_id",
                table: "price_list_channels",
                columns: new[] { "price_list_id", "sales_channel_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_price_list_items_product_id",
                table: "price_list_items",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "IX_price_list_items_product_variant_id",
                table: "price_list_items",
                column: "product_variant_id");

            migrationBuilder.CreateIndex(
                name: "uq_price_list_items_price_list_id_product_id_product_variant_id",
                table: "price_list_items",
                columns: new[] { "price_list_id", "product_id", "product_variant_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_price_list_outlets_outlet_id",
                table: "price_list_outlets",
                column: "outlet_id");

            migrationBuilder.CreateIndex(
                name: "uq_price_list_outlets_price_list_id_outlet_id",
                table: "price_list_outlets",
                columns: new[] { "price_list_id", "outlet_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_price_lists_tenant_id_price_list_code",
                table: "price_lists",
                columns: new[] { "tenant_id", "price_list_code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_product_attribute_definitions_tenant_id_attribute_code",
                table: "product_attribute_definitions",
                columns: new[] { "tenant_id", "attribute_code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_product_attribute_options_attribute_definition_id_option_code",
                table: "product_attribute_options",
                columns: new[] { "attribute_definition_id", "option_code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_product_attribute_value_options_attribute_option_id",
                table: "product_attribute_value_options",
                column: "attribute_option_id");

            migrationBuilder.CreateIndex(
                name: "uq_product_attribute_value_options_product_attribute_value_id_attribute_option_id",
                table: "product_attribute_value_options",
                columns: new[] { "product_attribute_value_id", "attribute_option_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_product_attribute_values_attribute_definition_id",
                table: "product_attribute_values",
                column: "attribute_definition_id");

            migrationBuilder.CreateIndex(
                name: "uq_product_attribute_values_product_id_attribute_definition_id",
                table: "product_attribute_values",
                columns: new[] { "product_id", "attribute_definition_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_product_barcodes_product_id",
                table: "product_barcodes",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "IX_product_barcodes_product_variant_id",
                table: "product_barcodes",
                column: "product_variant_id");

            migrationBuilder.CreateIndex(
                name: "uq_product_barcodes_tenant_id_barcode_value",
                table: "product_barcodes",
                columns: new[] { "tenant_id", "barcode_value" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_product_batches_product_id",
                table: "product_batches",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "IX_product_batches_product_variant_id",
                table: "product_batches",
                column: "product_variant_id");

            migrationBuilder.CreateIndex(
                name: "uq_product_batches_tenant_id_product_id_product_variant_id_batch_number",
                table: "product_batches",
                columns: new[] { "tenant_id", "product_id", "product_variant_id", "batch_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_product_categories_category_id",
                table: "product_categories",
                column: "category_id");

            migrationBuilder.CreateIndex(
                name: "uq_product_categories_product_id_category_id",
                table: "product_categories",
                columns: new[] { "product_id", "category_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_product_channel_visibility_sales_channel_id",
                table: "product_channel_visibility",
                column: "sales_channel_id");

            migrationBuilder.CreateIndex(
                name: "uq_product_channel_visibility_product_id_sales_channel_id",
                table: "product_channel_visibility",
                columns: new[] { "product_id", "sales_channel_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_product_choice_groups_choice_group_id",
                table: "product_choice_groups",
                column: "choice_group_id");

            migrationBuilder.CreateIndex(
                name: "uq_product_choice_groups_product_id_choice_group_id",
                table: "product_choice_groups",
                columns: new[] { "product_id", "choice_group_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_product_choice_options_choice_option_id",
                table: "product_choice_options",
                column: "choice_option_id");

            migrationBuilder.CreateIndex(
                name: "uq_product_choice_options_product_choice_group_id_choice_option_id",
                table: "product_choice_options",
                columns: new[] { "product_choice_group_id", "choice_option_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_product_collections_collection_id",
                table: "product_collections",
                column: "collection_id");

            migrationBuilder.CreateIndex(
                name: "uq_product_collections_product_id_collection_id",
                table: "product_collections",
                columns: new[] { "product_id", "collection_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_product_images_product_id_image_url",
                table: "product_images",
                columns: new[] { "product_id", "image_url" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_product_inventory_settings_product_variant_id",
                table: "product_inventory_settings",
                column: "product_variant_id");

            migrationBuilder.CreateIndex(
                name: "uq_product_inventory_settings_product_id_product_variant_id",
                table: "product_inventory_settings",
                columns: new[] { "product_id", "product_variant_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_product_option_template_values_product_option_template_id_value_code",
                table: "product_option_template_values",
                columns: new[] { "product_option_template_id", "value_code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_product_option_templates_tenant_id_template_code",
                table: "product_option_templates",
                columns: new[] { "tenant_id", "template_code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_product_option_values_product_option_id_option_value_code",
                table: "product_option_values",
                columns: new[] { "product_option_id", "option_value_code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_product_options_product_option_template_id",
                table: "product_options",
                column: "product_option_template_id");

            migrationBuilder.CreateIndex(
                name: "uq_product_options_product_id_option_code",
                table: "product_options",
                columns: new[] { "product_id", "option_code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_product_tax_assignments_tax_class_id",
                table: "product_tax_assignments",
                column: "tax_class_id");

            migrationBuilder.CreateIndex(
                name: "uq_product_tax_assignments_product_id_product_variant_id_tax_class_id",
                table: "product_tax_assignments",
                columns: new[] { "product_id", "product_variant_id", "tax_class_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_product_variant_option_values_product_option_value_id",
                table: "product_variant_option_values",
                column: "product_option_value_id");

            migrationBuilder.CreateIndex(
                name: "uq_product_variant_option_values_product_variant_id_product_option_value_id",
                table: "product_variant_option_values",
                columns: new[] { "product_variant_id", "product_option_value_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_product_variants_product_id",
                table: "product_variants",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "uq_product_variants_tenant_id_product_id_variant_code",
                table: "product_variants",
                columns: new[] { "tenant_id", "product_id", "variant_code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_product_variants_tenant_id_sku",
                table: "product_variants",
                columns: new[] { "tenant_id", "sku" },
                unique: true,
                filter: "sku IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "uq_products_tenant_id_product_code",
                table: "products",
                columns: new[] { "tenant_id", "product_code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_receipt_print_logs_receipt_id_attempt_number",
                table: "receipt_print_logs",
                columns: new[] { "receipt_id", "attempt_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_receipt_template_assignments_receipt_template_version_id",
                table: "receipt_template_assignments",
                column: "receipt_template_version_id");

            migrationBuilder.CreateIndex(
                name: "uq_receipt_template_versions_receipt_template_id_version_number",
                table: "receipt_template_versions",
                columns: new[] { "receipt_template_id", "version_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_receipt_templates_parent_template_id",
                table: "receipt_templates",
                column: "parent_template_id");

            migrationBuilder.CreateIndex(
                name: "uq_receipt_templates_tenant_id_template_code",
                table: "receipt_templates",
                columns: new[] { "tenant_id", "template_code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_receipts_document_number_sequence_id",
                table: "receipts",
                column: "document_number_sequence_id");

            migrationBuilder.CreateIndex(
                name: "IX_receipts_sales_order_id",
                table: "receipts",
                column: "sales_order_id");

            migrationBuilder.CreateIndex(
                name: "uq_receipts_tenant_id_receipt_number",
                table: "receipts",
                columns: new[] { "tenant_id", "receipt_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_return_inspections_sales_return_line_id",
                table: "return_inspections",
                column: "sales_return_line_id");

            migrationBuilder.CreateIndex(
                name: "uq_return_inspections_tenant_id_inspection_number",
                table: "return_inspections",
                columns: new[] { "tenant_id", "inspection_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_return_policies_tenant_id_policy_code",
                table: "return_policies",
                columns: new[] { "tenant_id", "policy_code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_return_reasons_tenant_id_reason_code",
                table: "return_reasons",
                columns: new[] { "tenant_id", "reason_code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_role_template_version_permissions_permission_definition_id",
                table: "role_template_version_permissions",
                column: "permission_definition_id");

            migrationBuilder.CreateIndex(
                name: "uq_role_template_version_permissions_role_template_version_id_permission_definition_id",
                table: "role_template_version_permissions",
                columns: new[] { "role_template_version_id", "permission_definition_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_role_template_versions_role_template_id_version_number",
                table: "role_template_versions",
                columns: new[] { "role_template_id", "version_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_role_templates_template_code",
                table: "role_templates",
                column: "template_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_sales_channels_tenant_id_channel_code",
                table: "sales_channels",
                columns: new[] { "tenant_id", "channel_code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_sales_exchange_events_sales_exchange_id_sequence_number",
                table: "sales_exchange_events",
                columns: new[] { "sales_exchange_id", "sequence_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_sales_exchange_lines_sales_exchange_id",
                table: "sales_exchange_lines",
                column: "sales_exchange_id");

            migrationBuilder.CreateIndex(
                name: "IX_sales_exchange_lines_sales_return_line_id",
                table: "sales_exchange_lines",
                column: "sales_return_line_id");

            migrationBuilder.CreateIndex(
                name: "IX_sales_exchanges_replacement_order_id",
                table: "sales_exchanges",
                column: "replacement_order_id");

            migrationBuilder.CreateIndex(
                name: "IX_sales_exchanges_sales_return_id",
                table: "sales_exchanges",
                column: "sales_return_id");

            migrationBuilder.CreateIndex(
                name: "uq_sales_exchanges_tenant_id_exchange_number",
                table: "sales_exchanges",
                columns: new[] { "tenant_id", "exchange_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_sales_order_charges_sales_order_id",
                table: "sales_order_charges",
                column: "sales_order_id");

            migrationBuilder.CreateIndex(
                name: "IX_sales_order_charges_sales_order_line_id",
                table: "sales_order_charges",
                column: "sales_order_line_id");

            migrationBuilder.CreateIndex(
                name: "IX_sales_order_discounts_sales_order_id",
                table: "sales_order_discounts",
                column: "sales_order_id");

            migrationBuilder.CreateIndex(
                name: "IX_sales_order_discounts_sales_order_line_id",
                table: "sales_order_discounts",
                column: "sales_order_line_id");

            migrationBuilder.CreateIndex(
                name: "IX_sales_order_line_components_sales_order_line_id",
                table: "sales_order_line_components",
                column: "sales_order_line_id");

            migrationBuilder.CreateIndex(
                name: "IX_sales_order_line_options_sales_order_line_id",
                table: "sales_order_line_options",
                column: "sales_order_line_id");

            migrationBuilder.CreateIndex(
                name: "uq_sales_order_line_status_history_sales_order_line_id_sequence_number",
                table: "sales_order_line_status_history",
                columns: new[] { "sales_order_line_id", "sequence_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_sales_order_lines_sales_order_id_line_number",
                table: "sales_order_lines",
                columns: new[] { "sales_order_id", "line_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_sales_order_status_history_sales_order_id_sequence_number",
                table: "sales_order_status_history",
                columns: new[] { "sales_order_id", "sequence_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_sales_order_taxes_sales_order_id",
                table: "sales_order_taxes",
                column: "sales_order_id");

            migrationBuilder.CreateIndex(
                name: "IX_sales_order_taxes_sales_order_line_id",
                table: "sales_order_taxes",
                column: "sales_order_line_id");

            migrationBuilder.CreateIndex(
                name: "IX_sales_orders_document_number_sequence_id",
                table: "sales_orders",
                column: "document_number_sequence_id");

            migrationBuilder.CreateIndex(
                name: "uq_sales_orders_tenant_id_order_number",
                table: "sales_orders",
                columns: new[] { "tenant_id", "order_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_sales_payment_events_sales_payment_id_sequence_number",
                table: "sales_payment_events",
                columns: new[] { "sales_payment_id", "sequence_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_sales_payment_transactions_sales_payment_id",
                table: "sales_payment_transactions",
                column: "sales_payment_id");

            migrationBuilder.CreateIndex(
                name: "uq_sales_payment_transactions_idempotency_key",
                table: "sales_payment_transactions",
                column: "idempotency_key",
                unique: true,
                filter: "idempotency_key IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_sales_payments_payment_method_id",
                table: "sales_payments",
                column: "payment_method_id");

            migrationBuilder.CreateIndex(
                name: "IX_sales_payments_sales_order_id",
                table: "sales_payments",
                column: "sales_order_id");

            migrationBuilder.CreateIndex(
                name: "uq_sales_payments_tenant_id_payment_number",
                table: "sales_payments",
                columns: new[] { "tenant_id", "payment_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_sales_refund_lines_sales_refund_id",
                table: "sales_refund_lines",
                column: "sales_refund_id");

            migrationBuilder.CreateIndex(
                name: "IX_sales_refund_lines_sales_return_line_id",
                table: "sales_refund_lines",
                column: "sales_return_line_id");

            migrationBuilder.CreateIndex(
                name: "IX_sales_refund_payment_allocations_original_sales_payment_id",
                table: "sales_refund_payment_allocations",
                column: "original_sales_payment_id");

            migrationBuilder.CreateIndex(
                name: "IX_sales_refund_payment_allocations_sales_refund_id",
                table: "sales_refund_payment_allocations",
                column: "sales_refund_id");

            migrationBuilder.CreateIndex(
                name: "IX_sales_refunds_sales_order_id",
                table: "sales_refunds",
                column: "sales_order_id");

            migrationBuilder.CreateIndex(
                name: "IX_sales_refunds_sales_return_id",
                table: "sales_refunds",
                column: "sales_return_id");

            migrationBuilder.CreateIndex(
                name: "uq_sales_refunds_tenant_id_refund_number",
                table: "sales_refunds",
                columns: new[] { "tenant_id", "refund_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_sales_return_events_sales_return_id_sequence_number",
                table: "sales_return_events",
                columns: new[] { "sales_return_id", "sequence_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_sales_return_lines_sales_order_line_id",
                table: "sales_return_lines",
                column: "sales_order_line_id");

            migrationBuilder.CreateIndex(
                name: "IX_sales_return_lines_sales_return_id",
                table: "sales_return_lines",
                column: "sales_return_id");

            migrationBuilder.CreateIndex(
                name: "IX_sales_returns_sales_order_id",
                table: "sales_returns",
                column: "sales_order_id");

            migrationBuilder.CreateIndex(
                name: "uq_sales_returns_tenant_id_return_number",
                table: "sales_returns",
                columns: new[] { "tenant_id", "return_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_serial_numbers_product_id",
                table: "serial_numbers",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "uq_serial_numbers_tenant_id_serial_number",
                table: "serial_numbers",
                columns: new[] { "tenant_id", "serial_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_setting_definitions_setting_key",
                table: "setting_definitions",
                column: "setting_key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_shopping_cart_item_components_shopping_cart_item_id",
                table: "shopping_cart_item_components",
                column: "shopping_cart_item_id");

            migrationBuilder.CreateIndex(
                name: "IX_shopping_cart_item_options_shopping_cart_item_id",
                table: "shopping_cart_item_options",
                column: "shopping_cart_item_id");

            migrationBuilder.CreateIndex(
                name: "uq_shopping_cart_items_shopping_cart_id_line_number",
                table: "shopping_cart_items",
                columns: new[] { "shopping_cart_id", "line_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_shopping_carts_tenant_id_cart_number",
                table: "shopping_carts",
                columns: new[] { "tenant_id", "cart_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_stock_adjustment_lines_product_id",
                table: "stock_adjustment_lines",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "uq_stock_adjustment_lines_stock_adjustment_id_line_number",
                table: "stock_adjustment_lines",
                columns: new[] { "stock_adjustment_id", "line_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_stock_adjustment_reasons_tenant_id_reason_code",
                table: "stock_adjustment_reasons",
                columns: new[] { "tenant_id", "reason_code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_stock_adjustments_tenant_id_adjustment_number",
                table: "stock_adjustments",
                columns: new[] { "tenant_id", "adjustment_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_stock_movement_cost_allocations_inventory_cost_layer_id",
                table: "stock_movement_cost_allocations",
                column: "inventory_cost_layer_id");

            migrationBuilder.CreateIndex(
                name: "IX_stock_movement_cost_allocations_stock_movement_id",
                table: "stock_movement_cost_allocations",
                column: "stock_movement_id");

            migrationBuilder.CreateIndex(
                name: "uq_stock_movement_references_stock_movement_id_reference_type_reference_id",
                table: "stock_movement_references",
                columns: new[] { "stock_movement_id", "reference_type", "reference_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_stock_movement_serials_serial_number_id",
                table: "stock_movement_serials",
                column: "serial_number_id");

            migrationBuilder.CreateIndex(
                name: "uq_stock_movement_serials_stock_movement_id_serial_number_id",
                table: "stock_movement_serials",
                columns: new[] { "stock_movement_id", "serial_number_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_stock_movements_tenant_id_movement_number",
                table: "stock_movements",
                columns: new[] { "tenant_id", "movement_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_stock_transfer_lines_product_id",
                table: "stock_transfer_lines",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "uq_stock_transfer_lines_stock_transfer_id_line_number",
                table: "stock_transfer_lines",
                columns: new[] { "stock_transfer_id", "line_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_stock_transfer_status_history_stock_transfer_id_sequence_number",
                table: "stock_transfer_status_history",
                columns: new[] { "stock_transfer_id", "sequence_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_stock_transfers_destination_inventory_location_id",
                table: "stock_transfers",
                column: "destination_inventory_location_id");

            migrationBuilder.CreateIndex(
                name: "IX_stock_transfers_source_inventory_location_id",
                table: "stock_transfers",
                column: "source_inventory_location_id");

            migrationBuilder.CreateIndex(
                name: "uq_stock_transfers_tenant_id_transfer_number",
                table: "stock_transfers",
                columns: new[] { "tenant_id", "transfer_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_stocktake_line_serials_serial_number_id",
                table: "stocktake_line_serials",
                column: "serial_number_id");

            migrationBuilder.CreateIndex(
                name: "uq_stocktake_line_serials_stocktake_line_id_serial_number_id",
                table: "stocktake_line_serials",
                columns: new[] { "stocktake_line_id", "serial_number_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_stocktake_lines_product_id",
                table: "stocktake_lines",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "uq_stocktake_lines_stocktake_session_id_product_id_product_variant_id_product_batch_id",
                table: "stocktake_lines",
                columns: new[] { "stocktake_session_id", "product_id", "product_variant_id", "product_batch_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_stocktake_sessions_inventory_location_id",
                table: "stocktake_sessions",
                column: "inventory_location_id");

            migrationBuilder.CreateIndex(
                name: "uq_stocktake_sessions_tenant_id_stocktake_number",
                table: "stocktake_sessions",
                columns: new[] { "tenant_id", "stocktake_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_subscription_addon_features_platform_feature_id",
                table: "subscription_addon_features",
                column: "platform_feature_id");

            migrationBuilder.CreateIndex(
                name: "uq_subscription_addon_features_subscription_addon_id_platform_feature_id",
                table: "subscription_addon_features",
                columns: new[] { "subscription_addon_id", "platform_feature_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_subscription_addon_limits_feature_limit_definition_id",
                table: "subscription_addon_limits",
                column: "feature_limit_definition_id");

            migrationBuilder.CreateIndex(
                name: "uq_subscription_addon_limits_subscription_addon_feature_id_feature_limit_definition_id",
                table: "subscription_addon_limits",
                columns: new[] { "subscription_addon_feature_id", "feature_limit_definition_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_subscription_addons_addon_code",
                table: "subscription_addons",
                column: "addon_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_subscription_credit_note_lines_subscription_credit_note_id_line_number",
                table: "subscription_credit_note_lines",
                columns: new[] { "subscription_credit_note_id", "line_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_subscription_credit_notes_subscription_invoice_id",
                table: "subscription_credit_notes",
                column: "subscription_invoice_id");

            migrationBuilder.CreateIndex(
                name: "uq_subscription_credit_notes_tenant_id_credit_note_number",
                table: "subscription_credit_notes",
                columns: new[] { "tenant_id", "credit_note_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_subscription_invoice_lines_subscription_invoice_id_line_number",
                table: "subscription_invoice_lines",
                columns: new[] { "subscription_invoice_id", "line_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_subscription_invoices_tenant_subscription_id",
                table: "subscription_invoices",
                column: "tenant_subscription_id");

            migrationBuilder.CreateIndex(
                name: "uq_subscription_invoices_tenant_id_invoice_number",
                table: "subscription_invoices",
                columns: new[] { "tenant_id", "invoice_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_subscription_payment_links_subscription_invoice_id",
                table: "subscription_payment_links",
                column: "subscription_invoice_id");

            migrationBuilder.CreateIndex(
                name: "uq_subscription_payment_links_payment_link_token_hash",
                table: "subscription_payment_links",
                column: "payment_link_token_hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_subscription_payment_transactions_subscription_invoice_id",
                table: "subscription_payment_transactions",
                column: "subscription_invoice_id");

            migrationBuilder.CreateIndex(
                name: "IX_subscription_payment_transactions_subscription_payment_link~",
                table: "subscription_payment_transactions",
                column: "subscription_payment_link_id");

            migrationBuilder.CreateIndex(
                name: "uq_subscription_payment_transactions_provider_transaction_reference",
                table: "subscription_payment_transactions",
                column: "provider_transaction_reference",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_subscription_plan_addons_subscription_addon_id",
                table: "subscription_plan_addons",
                column: "subscription_addon_id");

            migrationBuilder.CreateIndex(
                name: "uq_subscription_plan_addons_subscription_plan_id_subscription_addon_id",
                table: "subscription_plan_addons",
                columns: new[] { "subscription_plan_id", "subscription_addon_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_subscription_plan_feature_limits_feature_limit_definition_id",
                table: "subscription_plan_feature_limits",
                column: "feature_limit_definition_id");

            migrationBuilder.CreateIndex(
                name: "uq_subscription_plan_feature_limits_subscription_plan_feature_id_feature_limit_definition_id",
                table: "subscription_plan_feature_limits",
                columns: new[] { "subscription_plan_feature_id", "feature_limit_definition_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_subscription_plan_features_platform_feature_id",
                table: "subscription_plan_features",
                column: "platform_feature_id");

            migrationBuilder.CreateIndex(
                name: "uq_subscription_plan_features_subscription_plan_id_platform_feature_id",
                table: "subscription_plan_features",
                columns: new[] { "subscription_plan_id", "platform_feature_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_subscription_plans_plan_code",
                table: "subscription_plans",
                column: "plan_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_sync_batches_offline_client_id",
                table: "sync_batches",
                column: "offline_client_id");

            migrationBuilder.CreateIndex(
                name: "uq_sync_batches_tenant_id_offline_client_id_idempotency_key",
                table: "sync_batches",
                columns: new[] { "tenant_id", "offline_client_id", "idempotency_key" },
                unique: true,
                filter: "idempotency_key IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_sync_conflicts_offline_client_id",
                table: "sync_conflicts",
                column: "offline_client_id");

            migrationBuilder.CreateIndex(
                name: "IX_sync_conflicts_sync_batch_id",
                table: "sync_conflicts",
                column: "sync_batch_id");

            migrationBuilder.CreateIndex(
                name: "IX_sync_conflicts_sync_item_id",
                table: "sync_conflicts",
                column: "sync_item_id");

            migrationBuilder.CreateIndex(
                name: "IX_sync_items_offline_client_id",
                table: "sync_items",
                column: "offline_client_id");

            migrationBuilder.CreateIndex(
                name: "IX_sync_items_sync_batch_id",
                table: "sync_items",
                column: "sync_batch_id");

            migrationBuilder.CreateIndex(
                name: "uq_sync_items_tenant_id_offline_client_id_entity_name_client_record_id_operation_type_payload_hash",
                table: "sync_items",
                columns: new[] { "tenant_id", "offline_client_id", "entity_name", "client_record_id", "operation_type", "payload_hash" },
                unique: true,
                filter: "payload_hash IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_tax_class_rates_tax_rate_id",
                table: "tax_class_rates",
                column: "tax_rate_id");

            migrationBuilder.CreateIndex(
                name: "uq_tax_class_rates_tax_class_id_tax_rate_id",
                table: "tax_class_rates",
                columns: new[] { "tax_class_id", "tax_rate_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_tax_classes_tenant_id_tax_class_code",
                table: "tax_classes",
                columns: new[] { "tenant_id", "tax_class_code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_tax_jurisdictions_tenant_id_jurisdiction_code",
                table: "tax_jurisdictions",
                columns: new[] { "tenant_id", "jurisdiction_code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_tax_rates_tax_jurisdiction_id_tax_rate_code",
                table: "tax_rates",
                columns: new[] { "tax_jurisdiction_id", "tax_rate_code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tenant_addresses_tenant_id",
                table: "tenant_addresses",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_tenant_auth_sessions_tenant_user_id",
                table: "tenant_auth_sessions",
                column: "tenant_user_id");

            migrationBuilder.CreateIndex(
                name: "uq_tenant_auth_sessions_session_token_hash",
                table: "tenant_auth_sessions",
                column: "session_token_hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tenant_domains_tenant_id",
                table: "tenant_domains",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "uq_tenant_domains_domain_name",
                table: "tenant_domains",
                column: "domain_name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tenant_feature_entitlements_platform_feature_id",
                table: "tenant_feature_entitlements",
                column: "platform_feature_id");

            migrationBuilder.CreateIndex(
                name: "uq_tenant_feature_entitlements_tenant_id_platform_feature_id",
                table: "tenant_feature_entitlements",
                columns: new[] { "tenant_id", "platform_feature_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tenant_login_audits_tenant_user_id",
                table: "tenant_login_audits",
                column: "tenant_user_id");

            migrationBuilder.CreateIndex(
                name: "uq_tenant_profiles_tenant_id",
                table: "tenant_profiles",
                column: "tenant_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tenant_refresh_tokens_tenant_auth_session_id",
                table: "tenant_refresh_tokens",
                column: "tenant_auth_session_id");

            migrationBuilder.CreateIndex(
                name: "uq_tenant_refresh_tokens_token_hash",
                table: "tenant_refresh_tokens",
                column: "token_hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tenant_role_permissions_permission_definition_id",
                table: "tenant_role_permissions",
                column: "permission_definition_id");

            migrationBuilder.CreateIndex(
                name: "uq_tenant_role_permissions_tenant_role_id_permission_definition_id",
                table: "tenant_role_permissions",
                columns: new[] { "tenant_role_id", "permission_definition_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tenant_roles_role_template_version_id",
                table: "tenant_roles",
                column: "role_template_version_id");

            migrationBuilder.CreateIndex(
                name: "uq_tenant_roles_tenant_id_role_code",
                table: "tenant_roles",
                columns: new[] { "tenant_id", "role_code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tenant_settings_setting_definition_id",
                table: "tenant_settings",
                column: "setting_definition_id");

            migrationBuilder.CreateIndex(
                name: "uq_tenant_settings_tenant_id_setting_definition_id",
                table: "tenant_settings",
                columns: new[] { "tenant_id", "setting_definition_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tenant_subscription_addons_subscription_addon_id",
                table: "tenant_subscription_addons",
                column: "subscription_addon_id");

            migrationBuilder.CreateIndex(
                name: "uq_tenant_subscription_addons_tenant_subscription_id_subscription_addon_id",
                table: "tenant_subscription_addons",
                columns: new[] { "tenant_subscription_id", "subscription_addon_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_tenant_subscription_history_tenant_subscription_id_sequence_number",
                table: "tenant_subscription_history",
                columns: new[] { "tenant_subscription_id", "sequence_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tenant_subscriptions_subscription_plan_id",
                table: "tenant_subscriptions",
                column: "subscription_plan_id");

            migrationBuilder.CreateIndex(
                name: "uq_tenant_subscriptions_tenant_id_subscription_number",
                table: "tenant_subscriptions",
                columns: new[] { "tenant_id", "subscription_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tenant_usage_counters_platform_feature_id",
                table: "tenant_usage_counters",
                column: "platform_feature_id");

            migrationBuilder.CreateIndex(
                name: "uq_tenant_usage_counters_tenant_id_platform_feature_id_usage_period_start",
                table: "tenant_usage_counters",
                columns: new[] { "tenant_id", "platform_feature_id", "usage_period_start" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tenant_user_permissions_permission_definition_id",
                table: "tenant_user_permissions",
                column: "permission_definition_id");

            migrationBuilder.CreateIndex(
                name: "uq_tenant_user_permissions_tenant_user_id_permission_definition_id",
                table: "tenant_user_permissions",
                columns: new[] { "tenant_user_id", "permission_definition_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tenant_user_roles_tenant_role_id",
                table: "tenant_user_roles",
                column: "tenant_role_id");

            migrationBuilder.CreateIndex(
                name: "uq_tenant_user_roles_tenant_user_id_tenant_role_id",
                table: "tenant_user_roles",
                columns: new[] { "tenant_user_id", "tenant_role_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_tenant_users_tenant_id_normalized_email",
                table: "tenant_users",
                columns: new[] { "tenant_id", "normalized_email" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_tenant_users_tenant_id_normalized_phone",
                table: "tenant_users",
                columns: new[] { "tenant_id", "normalized_phone" },
                unique: true,
                filter: "normalized_phone IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_tenants_business_type_id",
                table: "tenants",
                column: "business_type_id");

            migrationBuilder.CreateIndex(
                name: "uq_tenants_primary_domain",
                table: "tenants",
                column: "primary_domain",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_tenants_tenant_code",
                table: "tenants",
                column: "tenant_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_till_cash_movements_till_session_id",
                table: "till_cash_movements",
                column: "till_session_id");

            migrationBuilder.CreateIndex(
                name: "IX_till_device_assignments_pos_device_id",
                table: "till_device_assignments",
                column: "pos_device_id");

            migrationBuilder.CreateIndex(
                name: "uq_till_device_assignments_till_id_pos_device_id_effective_from",
                table: "till_device_assignments",
                columns: new[] { "till_id", "pos_device_id", "effective_from" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_till_session_events_till_session_id",
                table: "till_session_events",
                column: "till_session_id");

            migrationBuilder.CreateIndex(
                name: "IX_till_session_payment_summaries_payment_method_id",
                table: "till_session_payment_summaries",
                column: "payment_method_id");

            migrationBuilder.CreateIndex(
                name: "uq_till_session_payment_summaries_till_session_summary_id_payment_method_id",
                table: "till_session_payment_summaries",
                columns: new[] { "till_session_summary_id", "payment_method_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_till_session_summaries_till_session_id",
                table: "till_session_summaries",
                column: "till_session_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_till_sessions_opened_by_tenant_user_id",
                table: "till_sessions",
                column: "opened_by_tenant_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_till_sessions_till_id",
                table: "till_sessions",
                column: "till_id");

            migrationBuilder.CreateIndex(
                name: "uq_till_sessions_tenant_id_till_id_session_number",
                table: "till_sessions",
                columns: new[] { "tenant_id", "till_id", "session_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tills_outlet_id",
                table: "tills",
                column: "outlet_id");

            migrationBuilder.CreateIndex(
                name: "uq_tills_tenant_id_outlet_id_till_code",
                table: "tills",
                columns: new[] { "tenant_id", "outlet_id", "till_code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_unit_of_measures_tenant_id_uom_code",
                table: "unit_of_measures",
                columns: new[] { "tenant_id", "uom_code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_user_invites_tenant_id_invite_token_hash",
                table: "user_invites",
                columns: new[] { "tenant_id", "invite_token_hash" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_user_setup_tokens_user_invite_id",
                table: "user_setup_tokens",
                column: "user_invite_id");

            migrationBuilder.CreateIndex(
                name: "uq_user_setup_tokens_token_hash",
                table: "user_setup_tokens",
                column: "token_hash",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "brands");

            migrationBuilder.DropTable(
                name: "business_type_option_templates");

            migrationBuilder.DropTable(
                name: "cash_count_denominations");

            migrationBuilder.DropTable(
                name: "cash_movements");

            migrationBuilder.DropTable(
                name: "checkout_events");

            migrationBuilder.DropTable(
                name: "checkout_session_line_components");

            migrationBuilder.DropTable(
                name: "checkout_session_line_options");

            migrationBuilder.DropTable(
                name: "choice_option_inventory_impacts");

            migrationBuilder.DropTable(
                name: "combo_components");

            migrationBuilder.DropTable(
                name: "combo_group_items");

            migrationBuilder.DropTable(
                name: "currencies");

            migrationBuilder.DropTable(
                name: "customer_consents");

            migrationBuilder.DropTable(
                name: "customer_password_reset_tokens");

            migrationBuilder.DropTable(
                name: "customer_refresh_tokens");

            migrationBuilder.DropTable(
                name: "departments");

            migrationBuilder.DropTable(
                name: "device_sync_states");

            migrationBuilder.DropTable(
                name: "discount_policy_channels");

            migrationBuilder.DropTable(
                name: "discount_policy_conditions");

            migrationBuilder.DropTable(
                name: "discount_policy_outlets");

            migrationBuilder.DropTable(
                name: "discount_policy_targets");

            migrationBuilder.DropTable(
                name: "email_verification_tokens");

            migrationBuilder.DropTable(
                name: "expiry_discount_applications");

            migrationBuilder.DropTable(
                name: "expiry_discount_rule_tiers");

            migrationBuilder.DropTable(
                name: "feature_flags");

            migrationBuilder.DropTable(
                name: "fulfillment_order_events");

            migrationBuilder.DropTable(
                name: "fulfillment_order_lines");

            migrationBuilder.DropTable(
                name: "hardware_device_assignments");

            migrationBuilder.DropTable(
                name: "hardware_profiles");

            migrationBuilder.DropTable(
                name: "hardware_test_logs");

            migrationBuilder.DropTable(
                name: "inventory_channel_allocations");

            migrationBuilder.DropTable(
                name: "inventory_reorder_rules");

            migrationBuilder.DropTable(
                name: "inventory_reservation_allocations");

            migrationBuilder.DropTable(
                name: "notification_delivery_attempts");

            migrationBuilder.DropTable(
                name: "notification_preferences");

            migrationBuilder.DropTable(
                name: "notification_read_receipts");

            migrationBuilder.DropTable(
                name: "offline_id_mappings");

            migrationBuilder.DropTable(
                name: "offline_number_blocks");

            migrationBuilder.DropTable(
                name: "outlet_addresses");

            migrationBuilder.DropTable(
                name: "outlet_business_hours");

            migrationBuilder.DropTable(
                name: "outlet_user_permissions");

            migrationBuilder.DropTable(
                name: "outlet_user_roles");

            migrationBuilder.DropTable(
                name: "password_reset_tokens");

            migrationBuilder.DropTable(
                name: "pickup_order_events");

            migrationBuilder.DropTable(
                name: "platform_integration_credentials");

            migrationBuilder.DropTable(
                name: "platform_integration_request_logs");

            migrationBuilder.DropTable(
                name: "platform_integration_webhook_events");

            migrationBuilder.DropTable(
                name: "platform_login_audits");

            migrationBuilder.DropTable(
                name: "platform_password_reset_tokens");

            migrationBuilder.DropTable(
                name: "platform_refresh_tokens");

            migrationBuilder.DropTable(
                name: "platform_role_permissions");

            migrationBuilder.DropTable(
                name: "platform_user_permissions");

            migrationBuilder.DropTable(
                name: "platform_user_roles");

            migrationBuilder.DropTable(
                name: "pos_order_holds");

            migrationBuilder.DropTable(
                name: "price_list_channels");

            migrationBuilder.DropTable(
                name: "price_list_items");

            migrationBuilder.DropTable(
                name: "price_list_outlets");

            migrationBuilder.DropTable(
                name: "product_attribute_value_options");

            migrationBuilder.DropTable(
                name: "product_barcodes");

            migrationBuilder.DropTable(
                name: "product_categories");

            migrationBuilder.DropTable(
                name: "product_channel_visibility");

            migrationBuilder.DropTable(
                name: "product_collections");

            migrationBuilder.DropTable(
                name: "product_images");

            migrationBuilder.DropTable(
                name: "product_inventory_settings");

            migrationBuilder.DropTable(
                name: "product_option_template_values");

            migrationBuilder.DropTable(
                name: "product_tax_assignments");

            migrationBuilder.DropTable(
                name: "product_variant_option_values");

            migrationBuilder.DropTable(
                name: "receipt_print_logs");

            migrationBuilder.DropTable(
                name: "receipt_template_assignments");

            migrationBuilder.DropTable(
                name: "return_inspections");

            migrationBuilder.DropTable(
                name: "return_policies");

            migrationBuilder.DropTable(
                name: "return_reasons");

            migrationBuilder.DropTable(
                name: "role_template_version_permissions");

            migrationBuilder.DropTable(
                name: "sales_exchange_events");

            migrationBuilder.DropTable(
                name: "sales_exchange_lines");

            migrationBuilder.DropTable(
                name: "sales_order_charges");

            migrationBuilder.DropTable(
                name: "sales_order_discounts");

            migrationBuilder.DropTable(
                name: "sales_order_line_components");

            migrationBuilder.DropTable(
                name: "sales_order_line_options");

            migrationBuilder.DropTable(
                name: "sales_order_line_status_history");

            migrationBuilder.DropTable(
                name: "sales_order_status_history");

            migrationBuilder.DropTable(
                name: "sales_order_taxes");

            migrationBuilder.DropTable(
                name: "sales_payment_events");

            migrationBuilder.DropTable(
                name: "sales_payment_transactions");

            migrationBuilder.DropTable(
                name: "sales_refund_lines");

            migrationBuilder.DropTable(
                name: "sales_refund_payment_allocations");

            migrationBuilder.DropTable(
                name: "sales_return_events");

            migrationBuilder.DropTable(
                name: "shopping_cart_item_components");

            migrationBuilder.DropTable(
                name: "shopping_cart_item_options");

            migrationBuilder.DropTable(
                name: "stock_adjustment_lines");

            migrationBuilder.DropTable(
                name: "stock_adjustment_reasons");

            migrationBuilder.DropTable(
                name: "stock_movement_cost_allocations");

            migrationBuilder.DropTable(
                name: "stock_movement_references");

            migrationBuilder.DropTable(
                name: "stock_movement_serials");

            migrationBuilder.DropTable(
                name: "stock_transfer_lines");

            migrationBuilder.DropTable(
                name: "stock_transfer_status_history");

            migrationBuilder.DropTable(
                name: "stocktake_line_serials");

            migrationBuilder.DropTable(
                name: "subscription_addon_limits");

            migrationBuilder.DropTable(
                name: "subscription_credit_note_lines");

            migrationBuilder.DropTable(
                name: "subscription_invoice_lines");

            migrationBuilder.DropTable(
                name: "subscription_payment_transactions");

            migrationBuilder.DropTable(
                name: "subscription_plan_addons");

            migrationBuilder.DropTable(
                name: "subscription_plan_feature_limits");

            migrationBuilder.DropTable(
                name: "sync_conflicts");

            migrationBuilder.DropTable(
                name: "tax_class_rates");

            migrationBuilder.DropTable(
                name: "tenant_addresses");

            migrationBuilder.DropTable(
                name: "tenant_domains");

            migrationBuilder.DropTable(
                name: "tenant_feature_entitlements");

            migrationBuilder.DropTable(
                name: "tenant_login_audits");

            migrationBuilder.DropTable(
                name: "tenant_profiles");

            migrationBuilder.DropTable(
                name: "tenant_refresh_tokens");

            migrationBuilder.DropTable(
                name: "tenant_role_permissions");

            migrationBuilder.DropTable(
                name: "tenant_settings");

            migrationBuilder.DropTable(
                name: "tenant_subscription_addons");

            migrationBuilder.DropTable(
                name: "tenant_subscription_history");

            migrationBuilder.DropTable(
                name: "tenant_usage_counters");

            migrationBuilder.DropTable(
                name: "tenant_user_permissions");

            migrationBuilder.DropTable(
                name: "tenant_user_roles");

            migrationBuilder.DropTable(
                name: "till_cash_movements");

            migrationBuilder.DropTable(
                name: "till_device_assignments");

            migrationBuilder.DropTable(
                name: "till_session_events");

            migrationBuilder.DropTable(
                name: "till_session_payment_summaries");

            migrationBuilder.DropTable(
                name: "unit_of_measures");

            migrationBuilder.DropTable(
                name: "user_setup_tokens");

            migrationBuilder.DropTable(
                name: "cash_reconciliations");

            migrationBuilder.DropTable(
                name: "cash_movement_types");

            migrationBuilder.DropTable(
                name: "checkout_session_lines");

            migrationBuilder.DropTable(
                name: "product_choice_options");

            migrationBuilder.DropTable(
                name: "combo_groups");

            migrationBuilder.DropTable(
                name: "customer_verification_otps");

            migrationBuilder.DropTable(
                name: "customer_auth_sessions");

            migrationBuilder.DropTable(
                name: "discount_policies");

            migrationBuilder.DropTable(
                name: "expiry_discount_rules");

            migrationBuilder.DropTable(
                name: "hardware_devices");

            migrationBuilder.DropTable(
                name: "inventory_balances");

            migrationBuilder.DropTable(
                name: "inventory_reservation_lines");

            migrationBuilder.DropTable(
                name: "notification_inbox_items");

            migrationBuilder.DropTable(
                name: "pickup_orders");

            migrationBuilder.DropTable(
                name: "platform_auth_sessions");

            migrationBuilder.DropTable(
                name: "platform_permissions");

            migrationBuilder.DropTable(
                name: "platform_roles");

            migrationBuilder.DropTable(
                name: "price_lists");

            migrationBuilder.DropTable(
                name: "product_attribute_options");

            migrationBuilder.DropTable(
                name: "product_attribute_values");

            migrationBuilder.DropTable(
                name: "categories");

            migrationBuilder.DropTable(
                name: "sales_channels");

            migrationBuilder.DropTable(
                name: "collections");

            migrationBuilder.DropTable(
                name: "product_option_values");

            migrationBuilder.DropTable(
                name: "receipts");

            migrationBuilder.DropTable(
                name: "receipt_template_versions");

            migrationBuilder.DropTable(
                name: "sales_exchanges");

            migrationBuilder.DropTable(
                name: "sales_return_lines");

            migrationBuilder.DropTable(
                name: "sales_payments");

            migrationBuilder.DropTable(
                name: "sales_refunds");

            migrationBuilder.DropTable(
                name: "shopping_cart_items");

            migrationBuilder.DropTable(
                name: "stock_adjustments");

            migrationBuilder.DropTable(
                name: "inventory_cost_layers");

            migrationBuilder.DropTable(
                name: "stock_movements");

            migrationBuilder.DropTable(
                name: "stock_transfers");

            migrationBuilder.DropTable(
                name: "serial_numbers");

            migrationBuilder.DropTable(
                name: "stocktake_lines");

            migrationBuilder.DropTable(
                name: "subscription_addon_features");

            migrationBuilder.DropTable(
                name: "subscription_credit_notes");

            migrationBuilder.DropTable(
                name: "subscription_payment_links");

            migrationBuilder.DropTable(
                name: "feature_limit_definitions");

            migrationBuilder.DropTable(
                name: "subscription_plan_features");

            migrationBuilder.DropTable(
                name: "sync_items");

            migrationBuilder.DropTable(
                name: "tax_classes");

            migrationBuilder.DropTable(
                name: "tax_rates");

            migrationBuilder.DropTable(
                name: "tenant_auth_sessions");

            migrationBuilder.DropTable(
                name: "setting_definitions");

            migrationBuilder.DropTable(
                name: "permission_definitions");

            migrationBuilder.DropTable(
                name: "tenant_roles");

            migrationBuilder.DropTable(
                name: "till_session_summaries");

            migrationBuilder.DropTable(
                name: "user_invites");

            migrationBuilder.DropTable(
                name: "checkout_sessions");

            migrationBuilder.DropTable(
                name: "choice_options");

            migrationBuilder.DropTable(
                name: "product_choice_groups");

            migrationBuilder.DropTable(
                name: "combo_definitions");

            migrationBuilder.DropTable(
                name: "customer_auth_accounts");

            migrationBuilder.DropTable(
                name: "discount_types");

            migrationBuilder.DropTable(
                name: "inventory_reservations");

            migrationBuilder.DropTable(
                name: "notification_messages");

            migrationBuilder.DropTable(
                name: "fulfillment_orders");

            migrationBuilder.DropTable(
                name: "pickup_slot_reservations");

            migrationBuilder.DropTable(
                name: "platform_users");

            migrationBuilder.DropTable(
                name: "product_attribute_definitions");

            migrationBuilder.DropTable(
                name: "product_options");

            migrationBuilder.DropTable(
                name: "receipt_templates");

            migrationBuilder.DropTable(
                name: "sales_order_lines");

            migrationBuilder.DropTable(
                name: "payment_methods");

            migrationBuilder.DropTable(
                name: "sales_returns");

            migrationBuilder.DropTable(
                name: "product_batches");

            migrationBuilder.DropTable(
                name: "stocktake_sessions");

            migrationBuilder.DropTable(
                name: "subscription_addons");

            migrationBuilder.DropTable(
                name: "subscription_invoices");

            migrationBuilder.DropTable(
                name: "platform_features");

            migrationBuilder.DropTable(
                name: "sync_batches");

            migrationBuilder.DropTable(
                name: "tax_jurisdictions");

            migrationBuilder.DropTable(
                name: "role_template_versions");

            migrationBuilder.DropTable(
                name: "till_sessions");

            migrationBuilder.DropTable(
                name: "shopping_carts");

            migrationBuilder.DropTable(
                name: "choice_groups");

            migrationBuilder.DropTable(
                name: "customers");

            migrationBuilder.DropTable(
                name: "notification_channels");

            migrationBuilder.DropTable(
                name: "notification_events");

            migrationBuilder.DropTable(
                name: "notification_template_versions");

            migrationBuilder.DropTable(
                name: "pickup_slots");

            migrationBuilder.DropTable(
                name: "product_option_templates");

            migrationBuilder.DropTable(
                name: "sales_orders");

            migrationBuilder.DropTable(
                name: "product_variants");

            migrationBuilder.DropTable(
                name: "inventory_locations");

            migrationBuilder.DropTable(
                name: "tenant_subscriptions");

            migrationBuilder.DropTable(
                name: "platform_modules");

            migrationBuilder.DropTable(
                name: "offline_clients");

            migrationBuilder.DropTable(
                name: "role_templates");

            migrationBuilder.DropTable(
                name: "tenant_users");

            migrationBuilder.DropTable(
                name: "tills");

            migrationBuilder.DropTable(
                name: "platform_integrations");

            migrationBuilder.DropTable(
                name: "notification_templates");

            migrationBuilder.DropTable(
                name: "fulfillment_method_outlets");

            migrationBuilder.DropTable(
                name: "document_number_sequences");

            migrationBuilder.DropTable(
                name: "products");

            migrationBuilder.DropTable(
                name: "subscription_plans");

            migrationBuilder.DropTable(
                name: "pos_devices");

            migrationBuilder.DropTable(
                name: "integration_providers");

            migrationBuilder.DropTable(
                name: "notification_event_types");

            migrationBuilder.DropTable(
                name: "fulfillment_methods");

            migrationBuilder.DropTable(
                name: "outlets");

            migrationBuilder.DropTable(
                name: "tenants");

            migrationBuilder.DropTable(
                name: "business_types");
        }
    }
}
