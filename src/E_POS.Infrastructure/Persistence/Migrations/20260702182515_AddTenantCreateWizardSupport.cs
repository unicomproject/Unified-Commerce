using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E_POS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTenantCreateWizardSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "tenant_user_id",
                table: "user_invites",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "first_name",
                table: "tenant_users",
                type: "varchar(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "last_name",
                table: "tenant_users",
                type: "varchar(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "auto_renew",
                table: "tenant_subscriptions",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<string>(
                name: "billing_cycle",
                table: "tenant_subscriptions",
                type: "varchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "monthly");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "billing_start_at",
                table: "tenant_subscriptions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "discount_type",
                table: "tenant_subscriptions",
                type: "varchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "discount_value",
                table: "tenant_subscriptions",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "invoice_email",
                table: "tenant_subscriptions",
                type: "varchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "max_outlets_override",
                table: "tenant_subscriptions",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "max_tills_override",
                table: "tenant_subscriptions",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "max_users_override",
                table: "tenant_subscriptions",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "next_billing_at",
                table: "tenant_subscriptions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "notes",
                table: "tenant_subscriptions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "payment_method",
                table: "tenant_subscriptions",
                type: "varchar(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "tax_percentage",
                table: "tenant_subscriptions",
                type: "numeric(8,2)",
                precision: 8,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "trial_end_at",
                table: "tenant_subscriptions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "trial_start_at",
                table: "tenant_subscriptions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "quantity",
                table: "tenant_subscription_addons",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<string>(
                name: "country_code",
                table: "tenant_profiles",
                type: "char(2)",
                maxLength: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "legal_name",
                table: "tenant_profiles",
                type: "varchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "primary_contact_name",
                table: "tenant_profiles",
                type: "varchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "primary_email",
                table: "tenant_profiles",
                type: "varchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "primary_phone",
                table: "tenant_profiles",
                type: "varchar(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "registration_number",
                table: "tenant_profiles",
                type: "varchar(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "tax_number",
                table: "tenant_profiles",
                type: "varchar(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "website_url",
                table: "tenant_profiles",
                type: "varchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "city",
                table: "tenant_addresses",
                type: "varchar(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "country_code",
                table: "tenant_addresses",
                type: "char(2)",
                maxLength: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "line1",
                table: "tenant_addresses",
                type: "varchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "line2",
                table: "tenant_addresses",
                type: "varchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "postal_code",
                table: "tenant_addresses",
                type: "varchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "state",
                table: "tenant_addresses",
                type: "varchar(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "billing_cycle",
                table: "subscription_invoices",
                type: "varchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "due_at",
                table: "subscription_invoices",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "invoice_status",
                table: "subscription_invoices",
                type: "varchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "DRAFT");

            migrationBuilder.CreateIndex(
                name: "IX_user_invites_tenant_user_id",
                table: "user_invites",
                column: "tenant_user_id");

            migrationBuilder.AddForeignKey(
                name: "fk_user_invites_tenant_user_id_tenant_users",
                table: "user_invites",
                column: "tenant_user_id",
                principalTable: "tenant_users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_user_invites_tenant_user_id_tenant_users",
                table: "user_invites");

            migrationBuilder.DropIndex(
                name: "IX_user_invites_tenant_user_id",
                table: "user_invites");

            migrationBuilder.DropColumn(
                name: "tenant_user_id",
                table: "user_invites");

            migrationBuilder.DropColumn(
                name: "first_name",
                table: "tenant_users");

            migrationBuilder.DropColumn(
                name: "last_name",
                table: "tenant_users");

            migrationBuilder.DropColumn(
                name: "auto_renew",
                table: "tenant_subscriptions");

            migrationBuilder.DropColumn(
                name: "billing_cycle",
                table: "tenant_subscriptions");

            migrationBuilder.DropColumn(
                name: "billing_start_at",
                table: "tenant_subscriptions");

            migrationBuilder.DropColumn(
                name: "discount_type",
                table: "tenant_subscriptions");

            migrationBuilder.DropColumn(
                name: "discount_value",
                table: "tenant_subscriptions");

            migrationBuilder.DropColumn(
                name: "invoice_email",
                table: "tenant_subscriptions");

            migrationBuilder.DropColumn(
                name: "max_outlets_override",
                table: "tenant_subscriptions");

            migrationBuilder.DropColumn(
                name: "max_tills_override",
                table: "tenant_subscriptions");

            migrationBuilder.DropColumn(
                name: "max_users_override",
                table: "tenant_subscriptions");

            migrationBuilder.DropColumn(
                name: "next_billing_at",
                table: "tenant_subscriptions");

            migrationBuilder.DropColumn(
                name: "notes",
                table: "tenant_subscriptions");

            migrationBuilder.DropColumn(
                name: "payment_method",
                table: "tenant_subscriptions");

            migrationBuilder.DropColumn(
                name: "tax_percentage",
                table: "tenant_subscriptions");

            migrationBuilder.DropColumn(
                name: "trial_end_at",
                table: "tenant_subscriptions");

            migrationBuilder.DropColumn(
                name: "trial_start_at",
                table: "tenant_subscriptions");

            migrationBuilder.DropColumn(
                name: "quantity",
                table: "tenant_subscription_addons");

            migrationBuilder.DropColumn(
                name: "country_code",
                table: "tenant_profiles");

            migrationBuilder.DropColumn(
                name: "legal_name",
                table: "tenant_profiles");

            migrationBuilder.DropColumn(
                name: "primary_contact_name",
                table: "tenant_profiles");

            migrationBuilder.DropColumn(
                name: "primary_email",
                table: "tenant_profiles");

            migrationBuilder.DropColumn(
                name: "primary_phone",
                table: "tenant_profiles");

            migrationBuilder.DropColumn(
                name: "registration_number",
                table: "tenant_profiles");

            migrationBuilder.DropColumn(
                name: "tax_number",
                table: "tenant_profiles");

            migrationBuilder.DropColumn(
                name: "website_url",
                table: "tenant_profiles");

            migrationBuilder.DropColumn(
                name: "city",
                table: "tenant_addresses");

            migrationBuilder.DropColumn(
                name: "country_code",
                table: "tenant_addresses");

            migrationBuilder.DropColumn(
                name: "line1",
                table: "tenant_addresses");

            migrationBuilder.DropColumn(
                name: "line2",
                table: "tenant_addresses");

            migrationBuilder.DropColumn(
                name: "postal_code",
                table: "tenant_addresses");

            migrationBuilder.DropColumn(
                name: "state",
                table: "tenant_addresses");

            migrationBuilder.DropColumn(
                name: "billing_cycle",
                table: "subscription_invoices");

            migrationBuilder.DropColumn(
                name: "due_at",
                table: "subscription_invoices");

            migrationBuilder.DropColumn(
                name: "invoice_status",
                table: "subscription_invoices");
        }
    }
}
