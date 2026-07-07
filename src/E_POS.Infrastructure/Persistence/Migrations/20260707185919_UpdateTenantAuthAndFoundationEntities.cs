using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E_POS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class UpdateTenantAuthAndFoundationEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_email_verification_tokens_tenant_user_id_tenant_users",
                table: "email_verification_tokens");

            migrationBuilder.DropForeignKey(
                name: "fk_outlet_user_permissions_permission_definition_id_permission_definitions",
                table: "outlet_user_permissions");

            migrationBuilder.DropForeignKey(
                name: "fk_outlet_user_permissions_tenant_user_id_tenant_users",
                table: "outlet_user_permissions");

            migrationBuilder.DropForeignKey(
                name: "fk_outlet_user_roles_tenant_role_id_tenant_roles",
                table: "outlet_user_roles");

            migrationBuilder.DropForeignKey(
                name: "fk_outlet_user_roles_tenant_user_id_tenant_users",
                table: "outlet_user_roles");

            migrationBuilder.DropForeignKey(
                name: "fk_password_reset_tokens_tenant_user_id_tenant_users",
                table: "password_reset_tokens");

            migrationBuilder.DropForeignKey(
                name: "fk_role_template_version_permissions_permission_definition_id_permission_definitions",
                table: "role_template_version_permissions");

            migrationBuilder.DropForeignKey(
                name: "fk_tenant_auth_sessions_tenant_user_id_tenant_users",
                table: "tenant_auth_sessions");

            migrationBuilder.DropForeignKey(
                name: "fk_tenant_login_audits_tenant_user_id_tenant_users",
                table: "tenant_login_audits");

            migrationBuilder.DropForeignKey(
                name: "fk_tenant_profiles_tenant_id_tenants",
                table: "tenant_profiles");

            migrationBuilder.DropForeignKey(
                name: "fk_tenant_role_permissions_permission_definition_id_permission_definitions",
                table: "tenant_role_permissions");

            migrationBuilder.DropForeignKey(
                name: "fk_tenant_role_permissions_tenant_role_id_tenant_roles",
                table: "tenant_role_permissions");

            migrationBuilder.DropForeignKey(
                name: "fk_tenant_roles_role_template_version_id_role_template_versions",
                table: "tenant_roles");

            migrationBuilder.DropForeignKey(
                name: "fk_tenant_user_permissions_permission_definition_id_permission_definitions",
                table: "tenant_user_permissions");

            migrationBuilder.DropForeignKey(
                name: "fk_tenant_user_permissions_tenant_user_id_tenant_users",
                table: "tenant_user_permissions");

            migrationBuilder.DropForeignKey(
                name: "fk_tenant_user_roles_tenant_role_id_tenant_roles",
                table: "tenant_user_roles");

            migrationBuilder.DropForeignKey(
                name: "fk_tenant_user_roles_tenant_user_id_tenant_users",
                table: "tenant_user_roles");

            migrationBuilder.DropForeignKey(
                name: "fk_tenants_business_type_id_business_types",
                table: "tenants");

            migrationBuilder.DropForeignKey(
                name: "fk_user_invites_tenant_user_id_tenant_users",
                table: "user_invites");

            migrationBuilder.DropForeignKey(
                name: "fk_user_setup_tokens_user_invite_id_user_invites",
                table: "user_setup_tokens");

            migrationBuilder.DropCheckConstraint(
                name: "ck_user_setup_tokens_status",
                table: "user_setup_tokens");

            migrationBuilder.DropIndex(
                name: "IX_user_invites_tenant_user_id",
                table: "user_invites");

            migrationBuilder.DropIndex(
                name: "uq_user_invites_tenant_id_invite_token_hash",
                table: "user_invites");

            migrationBuilder.DropCheckConstraint(
                name: "ck_user_invites_invite_status",
                table: "user_invites");

            migrationBuilder.DropIndex(
                name: "IX_tenants_business_type_id",
                table: "tenants");

            migrationBuilder.DropIndex(
                name: "uq_tenants_primary_domain",
                table: "tenants");

            migrationBuilder.DropIndex(
                name: "uq_tenant_users_normalized_email",
                table: "tenant_users");

            migrationBuilder.DropIndex(
                name: "uq_tenant_users_tenant_id_normalized_phone",
                table: "tenant_users");

            migrationBuilder.DropCheckConstraint(
                name: "ck_tenant_users_status",
                table: "tenant_users");

            migrationBuilder.DropIndex(
                name: "uq_tenant_user_roles_tenant_user_id_tenant_role_id",
                table: "tenant_user_roles");

            migrationBuilder.DropIndex(
                name: "uq_tenant_user_permissions_tenant_user_id_permission_definition_id",
                table: "tenant_user_permissions");

            migrationBuilder.DropCheckConstraint(
                name: "ck_tenant_roles_status",
                table: "tenant_roles");

            migrationBuilder.DropIndex(
                name: "uq_tenant_role_permissions_tenant_role_id_permission_definition_id",
                table: "tenant_role_permissions");

            migrationBuilder.DropIndex(
                name: "ix_tenant_refresh_tokens_tenant_auth_session_id_status",
                table: "tenant_refresh_tokens");

            migrationBuilder.DropCheckConstraint(
                name: "ck_tenant_refresh_tokens_expires_at_created_at",
                table: "tenant_refresh_tokens");

            migrationBuilder.DropCheckConstraint(
                name: "ck_tenant_refresh_tokens_status",
                table: "tenant_refresh_tokens");

            migrationBuilder.DropIndex(
                name: "ix_tenant_login_audits_tenant_user_id_login_result_created_at",
                table: "tenant_login_audits");

            migrationBuilder.DropCheckConstraint(
                name: "ck_tenant_login_audits_login_result",
                table: "tenant_login_audits");

            migrationBuilder.DropCheckConstraint(
                name: "ck_tenant_domains_domain_status",
                table: "tenant_domains");

            migrationBuilder.DropIndex(
                name: "uq_tenant_auth_sessions_session_token_hash",
                table: "tenant_auth_sessions");

            migrationBuilder.DropCheckConstraint(
                name: "ck_tenant_auth_sessions_status",
                table: "tenant_auth_sessions");

            migrationBuilder.DropCheckConstraint(
                name: "ck_setting_definitions_value_type",
                table: "setting_definitions");

            migrationBuilder.DropIndex(
                name: "uq_sales_channels_tenant_id_id",
                table: "sales_channels");

            migrationBuilder.DropCheckConstraint(
                name: "ck_sales_channels_channel_type",
                table: "sales_channels");

            migrationBuilder.DropCheckConstraint(
                name: "ck_role_templates_status",
                table: "role_templates");

            migrationBuilder.DropCheckConstraint(
                name: "ck_permission_definitions_status",
                table: "permission_definitions");

            migrationBuilder.DropIndex(
                name: "IX_password_reset_tokens_tenant_user_id",
                table: "password_reset_tokens");

            migrationBuilder.DropCheckConstraint(
                name: "ck_password_reset_tokens_status",
                table: "password_reset_tokens");

            migrationBuilder.DropIndex(
                name: "uq_outlet_user_roles_outlet_id_tenant_user_id_tenant_role_id",
                table: "outlet_user_roles");

            migrationBuilder.DropIndex(
                name: "uq_outlet_user_permissions_outlet_id_tenant_user_id_permission_definition_id",
                table: "outlet_user_permissions");

            migrationBuilder.DropIndex(
                name: "IX_email_verification_tokens_tenant_user_id",
                table: "email_verification_tokens");

            migrationBuilder.DropCheckConstraint(
                name: "ck_email_verification_tokens_status",
                table: "email_verification_tokens");

            migrationBuilder.DropIndex(
                name: "uq_business_types_business_type_key",
                table: "business_types");

            migrationBuilder.DropCheckConstraint(
                name: "ck_business_types_sort_order",
                table: "business_types");

            migrationBuilder.DropColumn(
                name: "status",
                table: "user_setup_tokens");

            migrationBuilder.DropColumn(
                name: "tenant_user_id",
                table: "user_invites");

            migrationBuilder.DropColumn(
                name: "base_currency",
                table: "tenants");

            migrationBuilder.DropColumn(
                name: "billing_status",
                table: "tenants");

            migrationBuilder.DropColumn(
                name: "business_type",
                table: "tenants");

            migrationBuilder.DropColumn(
                name: "business_type_id",
                table: "tenants");

            migrationBuilder.DropColumn(
                name: "default_locale",
                table: "tenants");

            migrationBuilder.DropColumn(
                name: "operating_mode",
                table: "tenants");

            migrationBuilder.DropColumn(
                name: "primary_domain",
                table: "tenants");

            migrationBuilder.DropColumn(
                name: "first_name",
                table: "tenant_users");

            migrationBuilder.DropColumn(
                name: "last_name",
                table: "tenant_users");

            migrationBuilder.RenameColumn(
                name: "normalized_email",
                table: "tenant_users",
                newName: "email");

            migrationBuilder.DropColumn(
                name: "normalized_phone",
                table: "tenant_users");

            migrationBuilder.DropColumn(
                name: "password_hash",
                table: "tenant_users");

            migrationBuilder.DropColumn(
                name: "status",
                table: "tenant_users");

            migrationBuilder.DropColumn(
                name: "description",
                table: "tenant_user_roles");

            migrationBuilder.DropColumn(
                name: "updated_at",
                table: "tenant_user_roles");

            migrationBuilder.DropColumn(
                name: "description",
                table: "tenant_user_permissions");

            migrationBuilder.DropColumn(
                name: "updated_at",
                table: "tenant_user_permissions");

            migrationBuilder.RenameColumn(
                name: "name",
                table: "tenant_roles",
                newName: "role_name");

            migrationBuilder.DropColumn(
                name: "status",
                table: "tenant_roles");

            migrationBuilder.DropColumn(
                name: "updated_at",
                table: "tenant_role_permissions");

            migrationBuilder.DropColumn(
                name: "status",
                table: "tenant_refresh_tokens");

            migrationBuilder.DropColumn(
                name: "country_code",
                table: "tenant_profiles");

            migrationBuilder.DropColumn(
                name: "registration_number",
                table: "tenant_profiles");

            migrationBuilder.DropColumn(
                name: "tax_number",
                table: "tenant_profiles");

            migrationBuilder.DropColumn(
                name: "domain_status",
                table: "tenant_domains");

            migrationBuilder.DropColumn(
                name: "session_token_hash",
                table: "tenant_auth_sessions");

            migrationBuilder.DropColumn(
                name: "status",
                table: "tenant_auth_sessions");

            migrationBuilder.DropColumn(
                name: "line1",
                table: "tenant_addresses");

            migrationBuilder.DropColumn(
                name: "line2",
                table: "tenant_addresses");

            migrationBuilder.DropColumn(
                name: "name",
                table: "sales_channels");

            migrationBuilder.DropColumn(
                name: "name",
                table: "role_templates");

            migrationBuilder.DropColumn(
                name: "sort_order",
                table: "role_templates");

            migrationBuilder.DropColumn(
                name: "status",
                table: "role_templates");

            migrationBuilder.DropColumn(
                name: "description",
                table: "role_template_version_permissions");

            migrationBuilder.DropColumn(
                name: "updated_at",
                table: "role_template_version_permissions");

            migrationBuilder.DropColumn(
                name: "name",
                table: "permission_definitions");

            migrationBuilder.DropColumn(
                name: "status",
                table: "permission_definitions");

            migrationBuilder.DropColumn(
                name: "status",
                table: "password_reset_tokens");

            migrationBuilder.DropColumn(
                name: "tenant_user_id",
                table: "password_reset_tokens");

            migrationBuilder.DropColumn(
                name: "description",
                table: "outlet_user_roles");

            migrationBuilder.DropColumn(
                name: "updated_at",
                table: "outlet_user_roles");

            migrationBuilder.DropColumn(
                name: "description",
                table: "outlet_user_permissions");

            migrationBuilder.DropColumn(
                name: "updated_at",
                table: "outlet_user_permissions");

            migrationBuilder.DropColumn(
                name: "status",
                table: "email_verification_tokens");

            migrationBuilder.DropColumn(
                name: "tenant_user_id",
                table: "email_verification_tokens");

            migrationBuilder.DropColumn(
                name: "name",
                table: "currencies");

            migrationBuilder.DropColumn(
                name: "business_type_key",
                table: "business_types");

            migrationBuilder.DropColumn(
                name: "is_system_type",
                table: "business_types");

            migrationBuilder.DropColumn(
                name: "sort_order",
                table: "business_types");

            migrationBuilder.RenameColumn(
                name: "user_invite_id",
                table: "user_setup_tokens",
                newName: "user_id");

            migrationBuilder.RenameIndex(
                name: "IX_user_setup_tokens_user_invite_id",
                table: "user_setup_tokens",
                newName: "IX_user_setup_tokens_user_id");

            migrationBuilder.RenameColumn(
                name: "name",
                table: "tenants",
                newName: "display_name");

            migrationBuilder.RenameColumn(
                name: "currency_code",
                table: "tenants",
                newName: "base_currency_code");

            migrationBuilder.RenameColumn(
                name: "tenant_user_id",
                table: "tenant_user_roles",
                newName: "user_id");

            migrationBuilder.RenameColumn(
                name: "tenant_role_id",
                table: "tenant_user_roles",
                newName: "role_id");

            migrationBuilder.RenameIndex(
                name: "IX_tenant_user_roles_tenant_role_id",
                table: "tenant_user_roles",
                newName: "IX_tenant_user_roles_role_id");

            migrationBuilder.RenameColumn(
                name: "tenant_user_id",
                table: "tenant_user_permissions",
                newName: "user_id");

            migrationBuilder.RenameColumn(
                name: "permission_definition_id",
                table: "tenant_user_permissions",
                newName: "permission_id");

            migrationBuilder.RenameIndex(
                name: "IX_tenant_user_permissions_permission_definition_id",
                table: "tenant_user_permissions",
                newName: "IX_tenant_user_permissions_permission_id");

            migrationBuilder.RenameColumn(
                name: "role_template_version_id",
                table: "tenant_roles",
                newName: "source_role_template_version_id");

            migrationBuilder.RenameColumn(
                name: "description",
                table: "tenant_roles",
                newName: "role_description");

            migrationBuilder.RenameIndex(
                name: "IX_tenant_roles_role_template_version_id",
                table: "tenant_roles",
                newName: "IX_tenant_roles_source_role_template_version_id");

            migrationBuilder.RenameColumn(
                name: "tenant_role_id",
                table: "tenant_role_permissions",
                newName: "role_id");

            migrationBuilder.RenameColumn(
                name: "permission_definition_id",
                table: "tenant_role_permissions",
                newName: "permission_id");

            migrationBuilder.RenameColumn(
                name: "description",
                table: "tenant_role_permissions",
                newName: "notes");

            migrationBuilder.RenameIndex(
                name: "IX_tenant_role_permissions_permission_definition_id",
                table: "tenant_role_permissions",
                newName: "IX_tenant_role_permissions_permission_id");

            migrationBuilder.RenameColumn(
                name: "tenant_user_id",
                table: "tenant_login_audits",
                newName: "user_id");

            migrationBuilder.RenameColumn(
                name: "login_result",
                table: "tenant_login_audits",
                newName: "attempted_identifier");

            migrationBuilder.RenameColumn(
                name: "tenant_user_id",
                table: "tenant_auth_sessions",
                newName: "revoked_by_tenant_user_id");

            migrationBuilder.RenameIndex(
                name: "IX_tenant_auth_sessions_tenant_user_id",
                table: "tenant_auth_sessions",
                newName: "IX_tenant_auth_sessions_revoked_by_tenant_user_id");

            migrationBuilder.RenameColumn(
                name: "state",
                table: "tenant_addresses",
                newName: "state_or_province");

            migrationBuilder.RenameIndex(
                name: "uq_sales_channels_tenant_id_channel_code",
                table: "sales_channels",
                newName: "ix_sales_channels_tenant_id_channel_code");

            migrationBuilder.RenameColumn(
                name: "permission_definition_id",
                table: "role_template_version_permissions",
                newName: "permission_id");

            // migrationBuilder.RenameIndex(
            //     name: "uq_role_template_version_permissions_role_template_version_id_permission_definition_id",
            //     table: "role_template_version_permissions",
            //     newName: "uq_role_template_version_permissions_role_template_version_id_permission_id");

            migrationBuilder.RenameIndex(
                name: "IX_role_template_version_permissions_permission_definition_id",
                table: "role_template_version_permissions",
                newName: "IX_role_template_version_permissions_permission_id");

            migrationBuilder.RenameColumn(
                name: "tenant_user_id",
                table: "outlet_user_roles",
                newName: "user_id");

            migrationBuilder.RenameColumn(
                name: "tenant_role_id",
                table: "outlet_user_roles",
                newName: "role_id");

            migrationBuilder.RenameIndex(
                name: "IX_outlet_user_roles_tenant_user_id",
                table: "outlet_user_roles",
                newName: "IX_outlet_user_roles_user_id");

            migrationBuilder.RenameIndex(
                name: "IX_outlet_user_roles_tenant_role_id",
                table: "outlet_user_roles",
                newName: "IX_outlet_user_roles_role_id");

            migrationBuilder.RenameColumn(
                name: "tenant_user_id",
                table: "outlet_user_permissions",
                newName: "user_id");

            migrationBuilder.RenameColumn(
                name: "permission_definition_id",
                table: "outlet_user_permissions",
                newName: "permission_id");

            migrationBuilder.RenameIndex(
                name: "IX_outlet_user_permissions_tenant_user_id",
                table: "outlet_user_permissions",
                newName: "IX_outlet_user_permissions_user_id");

            migrationBuilder.RenameIndex(
                name: "IX_outlet_user_permissions_permission_definition_id",
                table: "outlet_user_permissions",
                newName: "IX_outlet_user_permissions_permission_id");

            migrationBuilder.RenameColumn(
                name: "business_type_name",
                table: "business_types",
                newName: "business_name");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "expires_at",
                table: "user_setup_tokens",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<string>(
                name: "purpose",
                table: "user_setup_tokens",
                type: "varchar(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "revoked_at",
                table: "user_setup_tokens",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "tenant_id",
                table: "user_setup_tokens",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "used_at",
                table: "user_setup_tokens",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "invite_status",
                table: "user_invites",
                type: "varchar(40)",
                maxLength: 40,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(30)",
                oldMaxLength: 30);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "accepted_at",
                table: "user_invites",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "accepted_tenant_user_id",
                table: "user_invites",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "cancelled_at",
                table: "user_invites",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "expires_at",
                table: "user_invites",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<Guid>(
                name: "initial_outlet_id",
                table: "user_invites",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "initial_role_id",
                table: "user_invites",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "invited_by_platform_user_id",
                table: "user_invites",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "invited_by_tenant_user_id",
                table: "user_invites",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "invited_email",
                table: "user_invites",
                type: "varchar(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "invited_phone",
                table: "user_invites",
                type: "varchar(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "last_sent_at",
                table: "user_invites",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "normalized_invited_email",
                table: "user_invites",
                type: "varchar(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "normalized_invited_phone",
                table: "user_invites",
                type: "varchar(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "resend_count",
                table: "user_invites",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "sent_at",
                table: "user_invites",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "status",
                table: "tenants",
                type: "varchar(40)",
                maxLength: 40,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(30)",
                oldMaxLength: 30);

            migrationBuilder.AlterColumn<string>(
                name: "default_timezone",
                table: "tenants",
                type: "varchar(80)",
                maxLength: 80,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(100)",
                oldMaxLength: 100);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "activated_at",
                table: "tenants",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "archived_at",
                table: "tenants",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "created_by_platform_user_id",
                table: "tenants",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "data_region",
                table: "tenants",
                type: "varchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "suspended_at",
                table: "tenants",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "tenant_slug",
                table: "tenants",
                type: "varchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "updated_by_platform_user_id",
                table: "tenants",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "accepted_privacy_terms",
                table: "tenant_users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "accepted_terms_version",
                table: "tenant_users",
                type: "varchar(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "account_status",
                table: "tenant_users",
                type: "varchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "created_by_tenant_user_id",
                table: "tenant_users",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "default_outlet_id",
                table: "tenant_users",
                type: "varchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "display_name",
                table: "tenant_users",
                type: "varchar(500)",
                maxLength: 500,
                nullable: true);



            migrationBuilder.AddColumn<string>(
                name: "encrypted_password",
                table: "tenant_users",
                type: "varchar(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "failed_login_attempts",
                table: "tenant_users",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "full_name",
                table: "tenant_users",
                type: "varchar(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "locked_until",
                table: "tenant_users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "notes",
                table: "tenant_users",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "outlet_id",
                table: "tenant_users",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "password_change_required_at",
                table: "tenant_users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "password_salt",
                table: "tenant_users",
                type: "varchar(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "phone",
                table: "tenant_users",
                type: "varchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "profile_image_url",
                table: "tenant_users",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "source_user_type",
                table: "tenant_users",
                type: "varchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "admin");

            migrationBuilder.AddColumn<string>(
                name: "unmasked_phone",
                table: "tenant_users",
                type: "varchar(25)",
                maxLength: 25,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "updated_by_tenant_user_id",
                table: "tenant_users",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "user_type",
                table: "tenant_users",
                type: "varchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "standard");

            migrationBuilder.AlterColumn<Guid>(
                name: "user_id",
                table: "tenant_user_roles",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "assigned_at",
                table: "tenant_user_roles",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "assigned_by_tenant_user_id",
                table: "tenant_user_roles",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "revoked_at",
                table: "tenant_user_roles",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "tenant_id",
                table: "tenant_user_roles",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.Sql(@"
                UPDATE tenant_user_roles tur
                SET tenant_id = tr.tenant_id
                FROM tenant_roles tr
                WHERE tur.role_id = tr.id;
            ");

            migrationBuilder.AlterColumn<Guid>(
                name: "user_id",
                table: "tenant_user_permissions",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "assigned_at",
                table: "tenant_user_permissions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "assigned_by_tenant_user_id",
                table: "tenant_user_permissions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "revoked_at",
                table: "tenant_user_permissions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "tenant_id",
                table: "tenant_user_permissions",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "created_by_platform_user_id",
                table: "tenant_settings",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "setting_value",
                table: "tenant_settings",
                type: "jsonb",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "updated_by_platform_user_id",
                table: "tenant_settings",
                type: "uuid",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "role_code",
                table: "tenant_roles",
                type: "varchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(80)",
                oldMaxLength: 80);

            migrationBuilder.AddColumn<Guid>(
                name: "created_by_tenant_user_id",
                table: "tenant_roles",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_active",
                table: "tenant_roles",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "is_custom",
                table: "tenant_roles",
                type: "boolean",
                nullable: true);



            migrationBuilder.AddColumn<Guid>(
                name: "source_role_template_id",
                table: "tenant_roles",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "updated_by_tenant_user_id",
                table: "tenant_roles",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "granted_at",
                table: "tenant_role_permissions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "granted_by_tenant_user_id",
                table: "tenant_role_permissions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "revoked_at",
                table: "tenant_role_permissions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "revoked_by_tenant_user_id",
                table: "tenant_role_permissions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "tenant_id",
                table: "tenant_role_permissions",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.Sql(@"
                UPDATE tenant_role_permissions trp
                SET tenant_id = tr.tenant_id
                FROM tenant_roles tr
                WHERE trp.role_id = tr.id;
            ");

            migrationBuilder.AddColumn<Guid>(
                name: "replaced_by_token_id",
                table: "tenant_refresh_tokens",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "revoke_reason",
                table: "tenant_refresh_tokens",
                type: "varchar(250)",
                maxLength: 250,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "revoked_at",
                table: "tenant_refresh_tokens",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "revoked_by_platform_user_id",
                table: "tenant_refresh_tokens",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "revoked_by_tenant_user_id",
                table: "tenant_refresh_tokens",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "tenant_id",
                table: "tenant_refresh_tokens",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "token_family_id",
                table: "tenant_refresh_tokens",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "used_at",
                table: "tenant_refresh_tokens",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "user_id",
                table: "tenant_refresh_tokens",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AlterColumn<string>(
                name: "website_url",
                table: "tenant_profiles",
                type: "varchar(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(255)",
                oldMaxLength: 255,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "legal_name",
                table: "tenant_profiles",
                type: "varchar(250)",
                maxLength: 250,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "varchar(200)",
                oldMaxLength: 200,
                oldNullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "business_type_id",
                table: "tenant_profiles",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "created_by_platform_user_id",
                table: "tenant_profiles",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "description",
                table: "tenant_profiles",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "logo_url",
                table: "tenant_profiles",
                type: "varchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "trading_name",
                table: "tenant_profiles",
                type: "varchar(250)",
                maxLength: 250,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "updated_by_platform_user_id",
                table: "tenant_profiles",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "attempted_at",
                table: "tenant_login_audits",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<Guid>(
                name: "auth_session_id",
                table: "tenant_login_audits",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "authentication_method",
                table: "tenant_login_audits",
                type: "varchar(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "failure_code",
                table: "tenant_login_audits",
                type: "varchar(60)",
                maxLength: 60,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "failure_detail",
                table: "tenant_login_audits",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ip_address",
                table: "tenant_login_audits",
                type: "varchar(45)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "login_status",
                table: "tenant_login_audits",
                type: "varchar(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "pos_device_id",
                table: "tenant_login_audits",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "tenant_id",
                table: "tenant_login_audits",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "user_agent",
                table: "tenant_login_audits",
                type: "text",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "domain_name",
                table: "tenant_domains",
                type: "varchar(255)",
                maxLength: 255,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(200)",
                oldMaxLength: 200);

            migrationBuilder.AddColumn<Guid>(
                name: "created_by_platform_user_id",
                table: "tenant_domains",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "domain_type",
                table: "tenant_domains",
                type: "varchar(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "is_primary",
                table: "tenant_domains",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ssl_expires_at",
                table: "tenant_domains",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ssl_issued_at",
                table: "tenant_domains",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ssl_status",
                table: "tenant_domains",
                type: "varchar(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "status",
                table: "tenant_domains",
                type: "varchar(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "updated_by_platform_user_id",
                table: "tenant_domains",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "verification_status",
                table: "tenant_domains",
                type: "varchar(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "verification_token_hash",
                table: "tenant_domains",
                type: "varchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "verified_at",
                table: "tenant_domains",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "device_name",
                table: "tenant_auth_sessions",
                type: "varchar(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "expires_at",
                table: "tenant_auth_sessions",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<string>(
                name: "ip_address",
                table: "tenant_auth_sessions",
                type: "varchar(45)",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "last_seen_at",
                table: "tenant_auth_sessions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "pos_device_id",
                table: "tenant_auth_sessions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "revoke_reason",
                table: "tenant_auth_sessions",
                type: "varchar(250)",
                maxLength: 250,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "revoked_at",
                table: "tenant_auth_sessions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "revoked_by_platform_user_id",
                table: "tenant_auth_sessions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "tenant_id",
                table: "tenant_auth_sessions",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "user_agent",
                table: "tenant_auth_sessions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "user_id",
                table: "tenant_auth_sessions",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AlterColumn<string>(
                name: "country_code",
                table: "tenant_addresses",
                type: "char(2)",
                maxLength: 2,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "char(2)",
                oldMaxLength: 2,
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "address_line1",
                table: "tenant_addresses",
                type: "varchar(250)",
                maxLength: 250,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "address_line2",
                table: "tenant_addresses",
                type: "varchar(250)",
                maxLength: 250,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "created_by_platform_user_id",
                table: "tenant_addresses",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_primary",
                table: "tenant_addresses",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "status",
                table: "tenant_addresses",
                type: "varchar(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "updated_by_platform_user_id",
                table: "tenant_addresses",
                type: "uuid",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "value_type",
                table: "setting_definitions",
                type: "varchar(30)",
                maxLength: 30,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(40)",
                oldMaxLength: 40);

            migrationBuilder.AlterColumn<string>(
                name: "setting_key",
                table: "setting_definitions",
                type: "varchar(120)",
                maxLength: 120,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(255)",
                oldMaxLength: 255);

            migrationBuilder.AddColumn<string>(
                name: "default_value",
                table: "setting_definitions",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "description",
                table: "setting_definitions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "display_name",
                table: "setting_definitions",
                type: "varchar(150)",
                maxLength: 150,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "is_tenant_editable",
                table: "setting_definitions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "status",
                table: "setting_definitions",
                type: "varchar(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<string>(
                name: "status",
                table: "sales_channels",
                type: "varchar(40)",
                maxLength: 40,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(30)",
                oldMaxLength: 30);

            migrationBuilder.AddColumn<string>(
                name: "channel_mode",
                table: "sales_channels",
                type: "varchar(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "channel_name",
                table: "sales_channels",
                type: "varchar(150)",
                maxLength: 150,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<string>(
                name: "template_code",
                table: "role_templates",
                type: "varchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(80)",
                oldMaxLength: 80);

            migrationBuilder.AddColumn<Guid>(
                name: "created_by_tenant_user_id",
                table: "role_templates",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_active",
                table: "role_templates",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "is_default",
                table: "role_templates",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "template_name",
                table: "role_templates",
                type: "varchar(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "updated_by_tenant_user_id",
                table: "role_templates",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "created_by_tenant_user_id",
                table: "role_template_versions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "effective_from",
                table: "role_template_versions",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "effective_until",
                table: "role_template_versions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_active",
                table: "role_template_versions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "version_label",
                table: "role_template_versions",
                type: "varchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_active",
                table: "role_template_version_permissions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AlterColumn<string>(
                name: "permission_code",
                table: "permission_definitions",
                type: "varchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(80)",
                oldMaxLength: 80);

            migrationBuilder.AddColumn<string>(
                name: "action_type",
                table: "permission_definitions",
                type: "varchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "description",
                table: "permission_definitions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "feature_id",
                table: "permission_definitions",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<bool>(
                name: "is_active",
                table: "permission_definitions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "is_system",
                table: "permission_definitions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "module_id",
                table: "permission_definitions",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "expires_at",
                table: "password_reset_tokens",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<string>(
                name: "requested_ip_address",
                table: "password_reset_tokens",
                type: "varchar(45)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "requested_user_agent",
                table: "password_reset_tokens",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "revoked_at",
                table: "password_reset_tokens",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "tenant_id",
                table: "password_reset_tokens",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "used_at",
                table: "password_reset_tokens",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "user_id",
                table: "password_reset_tokens",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AlterColumn<Guid>(
                name: "outlet_id",
                table: "outlet_user_roles",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "user_id",
                table: "outlet_user_roles",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "assigned_at",
                table: "outlet_user_roles",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "assigned_by_tenant_user_id",
                table: "outlet_user_roles",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "revoked_at",
                table: "outlet_user_roles",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "revoked_by_tenant_user_id",
                table: "outlet_user_roles",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "tenant_id",
                table: "outlet_user_roles",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AlterColumn<Guid>(
                name: "outlet_id",
                table: "outlet_user_permissions",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "user_id",
                table: "outlet_user_permissions",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "assigned_at",
                table: "outlet_user_permissions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "assigned_by_tenant_user_id",
                table: "outlet_user_permissions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "revoked_at",
                table: "outlet_user_permissions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "revoked_by_tenant_user_id",
                table: "outlet_user_permissions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "tenant_id",
                table: "outlet_user_permissions",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "email_to_verify",
                table: "email_verification_tokens",
                type: "varchar(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "expires_at",
                table: "email_verification_tokens",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<string>(
                name: "normalized_email_to_verify",
                table: "email_verification_tokens",
                type: "varchar(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "revoked_at",
                table: "email_verification_tokens",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "tenant_id",
                table: "email_verification_tokens",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "user_id",
                table: "email_verification_tokens",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "verified_at",
                table: "email_verification_tokens",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AlterColumn<short>(
                name: "decimal_places",
                table: "currencies",
                type: "smallint",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<string>(
                name: "currency_name",
                table: "currencies",
                type: "varchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "currency_symbol",
                table: "currencies",
                type: "varchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_active",
                table: "currencies",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "sort_order",
                table: "currencies",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "business_code",
                table: "business_types",
                type: "varchar(80)",
                maxLength: 80,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_user_setup_tokens_tenant_id",
                table: "user_setup_tokens",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_user_invites_accepted_tenant_user_id",
                table: "user_invites",
                column: "accepted_tenant_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_user_invites_initial_role_id",
                table: "user_invites",
                column: "initial_role_id");

            migrationBuilder.CreateIndex(
                name: "uq_user_invites_invite_token_hash",
                table: "user_invites",
                column: "invite_token_hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_user_invites_tenant_id_normalized_invited_email",
                table: "user_invites",
                columns: new[] { "tenant_id", "normalized_invited_email" },
                unique: true,
                filter: "invite_status IN ('PENDING', 'SENT')");

            migrationBuilder.CreateIndex(
                name: "IX_tenants_base_currency_code",
                table: "tenants",
                column: "base_currency_code");

            migrationBuilder.CreateIndex(
                name: "uq_tenants_tenant_slug",
                table: "tenants",
                column: "tenant_slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tenant_users_created_by_tenant_user_id",
                table: "tenant_users",
                column: "created_by_tenant_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_tenant_users_updated_by_tenant_user_id",
                table: "tenant_users",
                column: "updated_by_tenant_user_id");

            migrationBuilder.CreateIndex(
                name: "uq_tenant_users_tenant_id_email",
                table: "tenant_users",
                columns: new[] { "tenant_id", "email" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_tenant_users_tenant_id_unmasked_phone",
                table: "tenant_users",
                columns: new[] { "tenant_id", "unmasked_phone" },
                unique: true,
                filter: "unmasked_phone IS NOT NULL");

            migrationBuilder.AddCheckConstraint(
                name: "ck_tenant_users_locked_until",
                table: "tenant_users",
                sql: "locked_until IS NULL OR locked_until > now()");

            migrationBuilder.AddCheckConstraint(
                name: "ck_tenant_users_source_user_type",
                table: "tenant_users",
                sql: "source_user_type IN ('admin', 'outlet', 'platform')");

            migrationBuilder.CreateIndex(
                name: "IX_tenant_user_roles_assigned_by_tenant_user_id",
                table: "tenant_user_roles",
                column: "assigned_by_tenant_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_tenant_user_roles_user_id",
                table: "tenant_user_roles",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "uq_tenant_user_roles_tenant_id_user_id_role_id",
                table: "tenant_user_roles",
                columns: new[] { "tenant_id", "user_id", "role_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tenant_user_permissions_assigned_by_tenant_user_id",
                table: "tenant_user_permissions",
                column: "assigned_by_tenant_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_tenant_user_permissions_user_id",
                table: "tenant_user_permissions",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "uq_tenant_user_permissions_tenant_id_user_id_permission_id",
                table: "tenant_user_permissions",
                columns: new[] { "tenant_id", "user_id", "permission_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tenant_roles_created_by_tenant_user_id",
                table: "tenant_roles",
                column: "created_by_tenant_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_tenant_roles_source_role_template_id",
                table: "tenant_roles",
                column: "source_role_template_id");

            migrationBuilder.CreateIndex(
                name: "IX_tenant_roles_updated_by_tenant_user_id",
                table: "tenant_roles",
                column: "updated_by_tenant_user_id");

            migrationBuilder.CreateIndex(
                name: "uq_tenant_roles_tenant_id_role_name",
                table: "tenant_roles",
                columns: new[] { "tenant_id", "role_name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tenant_role_permissions_granted_by_tenant_user_id",
                table: "tenant_role_permissions",
                column: "granted_by_tenant_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_tenant_role_permissions_revoked_by_tenant_user_id",
                table: "tenant_role_permissions",
                column: "revoked_by_tenant_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_tenant_role_permissions_role_id",
                table: "tenant_role_permissions",
                column: "role_id");

            migrationBuilder.CreateIndex(
                name: "uq_tenant_role_permissions_tenant_id_role_id_permission_id",
                table: "tenant_role_permissions",
                columns: new[] { "tenant_id", "role_id", "permission_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tenant_refresh_tokens_revoked_by_tenant_user_id",
                table: "tenant_refresh_tokens",
                column: "revoked_by_tenant_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_tenant_refresh_tokens_tenant_auth_session_id",
                table: "tenant_refresh_tokens",
                column: "tenant_auth_session_id");

            migrationBuilder.CreateIndex(
                name: "IX_tenant_refresh_tokens_tenant_id",
                table: "tenant_refresh_tokens",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_tenant_refresh_tokens_user_id",
                table: "tenant_refresh_tokens",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "uq_tenant_refresh_tokens_replaced_by_token_id",
                table: "tenant_refresh_tokens",
                column: "replaced_by_token_id",
                unique: true,
                filter: "replaced_by_token_id IS NOT NULL");

            migrationBuilder.AddCheckConstraint(
                name: "ck_tenant_refresh_tokens_revoked_by",
                table: "tenant_refresh_tokens",
                sql: "NOT (revoked_by_tenant_user_id IS NOT NULL AND revoked_by_platform_user_id IS NOT NULL)");

            migrationBuilder.CreateIndex(
                name: "IX_tenant_login_audits_auth_session_id",
                table: "tenant_login_audits",
                column: "auth_session_id");

            migrationBuilder.CreateIndex(
                name: "IX_tenant_login_audits_tenant_id",
                table: "tenant_login_audits",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_tenant_login_audits_user_id",
                table: "tenant_login_audits",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "uq_tenant_domains_verification_token_hash",
                table: "tenant_domains",
                column: "verification_token_hash",
                unique: true,
                filter: "verification_token_hash IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_tenant_auth_sessions_tenant_id",
                table: "tenant_auth_sessions",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_tenant_auth_sessions_user_id",
                table: "tenant_auth_sessions",
                column: "user_id");

            migrationBuilder.AddCheckConstraint(
                name: "ck_tenant_auth_sessions_revoked_by",
                table: "tenant_auth_sessions",
                sql: "NOT (revoked_by_tenant_user_id IS NOT NULL AND revoked_by_platform_user_id IS NOT NULL)");

            migrationBuilder.AddCheckConstraint(
                name: "ck_sales_channels_channel_mode",
                table: "sales_channels",
                sql: "channel_mode IS NULL OR channel_mode IN ('ONLINE', 'OFFLINE', 'HYBRID')");

            migrationBuilder.AddCheckConstraint(
                name: "ck_sales_channels_channel_type",
                table: "sales_channels",
                sql: "channel_type IN ('POS', 'E_COMMERCE')");

            migrationBuilder.AddCheckConstraint(
                name: "ck_sales_channels_sort_order",
                table: "sales_channels",
                sql: "sort_order >= 0");

            migrationBuilder.CreateIndex(
                name: "IX_role_templates_created_by_tenant_user_id",
                table: "role_templates",
                column: "created_by_tenant_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_role_templates_updated_by_tenant_user_id",
                table: "role_templates",
                column: "updated_by_tenant_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_role_template_versions_created_by_tenant_user_id",
                table: "role_template_versions",
                column: "created_by_tenant_user_id");

            migrationBuilder.AddCheckConstraint(
                name: "ck_role_template_versions_effective_dates",
                table: "role_template_versions",
                sql: "effective_until IS NULL OR effective_until > effective_from");

            migrationBuilder.CreateIndex(
                name: "IX_password_reset_tokens_tenant_id",
                table: "password_reset_tokens",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_password_reset_tokens_user_id",
                table: "password_reset_tokens",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_outlet_user_roles_assigned_by_tenant_user_id",
                table: "outlet_user_roles",
                column: "assigned_by_tenant_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_outlet_user_roles_outlet_id",
                table: "outlet_user_roles",
                column: "outlet_id");

            migrationBuilder.CreateIndex(
                name: "IX_outlet_user_roles_revoked_by_tenant_user_id",
                table: "outlet_user_roles",
                column: "revoked_by_tenant_user_id");

            migrationBuilder.CreateIndex(
                name: "uq_outlet_user_roles_tenant_outlet_user_role",
                table: "outlet_user_roles",
                columns: new[] { "tenant_id", "outlet_id", "user_id", "role_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_outlet_user_permissions_assigned_by_tenant_user_id",
                table: "outlet_user_permissions",
                column: "assigned_by_tenant_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_outlet_user_permissions_outlet_id",
                table: "outlet_user_permissions",
                column: "outlet_id");

            migrationBuilder.CreateIndex(
                name: "IX_outlet_user_permissions_revoked_by_tenant_user_id",
                table: "outlet_user_permissions",
                column: "revoked_by_tenant_user_id");

            migrationBuilder.CreateIndex(
                name: "uq_outlet_user_permissions_tenant_outlet_user_permission",
                table: "outlet_user_permissions",
                columns: new[] { "tenant_id", "outlet_id", "user_id", "permission_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_email_verification_tokens_tenant_id",
                table: "email_verification_tokens",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_email_verification_tokens_user_id",
                table: "email_verification_tokens",
                column: "user_id");

            migrationBuilder.AddCheckConstraint(
                name: "ck_currencies_sort_order",
                table: "currencies",
                sql: "sort_order >= 0");

            migrationBuilder.CreateIndex(
                name: "ix_business_types_business_code",
                table: "business_types",
                column: "business_code",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "fk_email_verification_tokens_tenant_id_tenants",
                table: "email_verification_tokens",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_email_verification_tokens_user_id_tenant_users",
                table: "email_verification_tokens",
                column: "user_id",
                principalTable: "tenant_users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_outlet_user_permissions_assigned_by",
                table: "outlet_user_permissions",
                column: "assigned_by_tenant_user_id",
                principalTable: "tenant_users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_outlet_user_permissions_permission_id_permission_definitions",
                table: "outlet_user_permissions",
                column: "permission_id",
                principalTable: "permission_definitions",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_outlet_user_permissions_revoked_by",
                table: "outlet_user_permissions",
                column: "revoked_by_tenant_user_id",
                principalTable: "tenant_users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_outlet_user_permissions_tenant_id_tenants",
                table: "outlet_user_permissions",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_outlet_user_permissions_user_id_tenant_users",
                table: "outlet_user_permissions",
                column: "user_id",
                principalTable: "tenant_users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_outlet_user_roles_assigned_by",
                table: "outlet_user_roles",
                column: "assigned_by_tenant_user_id",
                principalTable: "tenant_users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_outlet_user_roles_revoked_by",
                table: "outlet_user_roles",
                column: "revoked_by_tenant_user_id",
                principalTable: "tenant_users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_outlet_user_roles_role_id_tenant_roles",
                table: "outlet_user_roles",
                column: "role_id",
                principalTable: "tenant_roles",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_outlet_user_roles_tenant_id_tenants",
                table: "outlet_user_roles",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_outlet_user_roles_user_id_tenant_users",
                table: "outlet_user_roles",
                column: "user_id",
                principalTable: "tenant_users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_password_reset_tokens_tenant_id_tenants",
                table: "password_reset_tokens",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_password_reset_tokens_user_id_tenant_users",
                table: "password_reset_tokens",
                column: "user_id",
                principalTable: "tenant_users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_role_template_version_permissions_permission_id_permission_definitions",
                table: "role_template_version_permissions",
                column: "permission_id",
                principalTable: "permission_definitions",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_role_template_versions_created_by",
                table: "role_template_versions",
                column: "created_by_tenant_user_id",
                principalTable: "tenant_users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_role_templates_created_by",
                table: "role_templates",
                column: "created_by_tenant_user_id",
                principalTable: "tenant_users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_role_templates_updated_by",
                table: "role_templates",
                column: "updated_by_tenant_user_id",
                principalTable: "tenant_users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_tenant_auth_sessions_revoked_by_tenant_user_id_tenant_users",
                table: "tenant_auth_sessions",
                column: "revoked_by_tenant_user_id",
                principalTable: "tenant_users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_tenant_auth_sessions_tenant_id_tenants",
                table: "tenant_auth_sessions",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_tenant_auth_sessions_user_id_tenant_users",
                table: "tenant_auth_sessions",
                column: "user_id",
                principalTable: "tenant_users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_tenant_login_audits_auth_session_id_tenant_auth_sessions",
                table: "tenant_login_audits",
                column: "auth_session_id",
                principalTable: "tenant_auth_sessions",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_tenant_login_audits_tenant_id_tenants",
                table: "tenant_login_audits",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_tenant_login_audits_user_id_tenant_users",
                table: "tenant_login_audits",
                column: "user_id",
                principalTable: "tenant_users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_tenant_profiles_tenant_id_tenants",
                table: "tenant_profiles",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_tenant_refresh_tokens_replaced_by_token_id_tenant_refresh_tokens",
                table: "tenant_refresh_tokens",
                column: "replaced_by_token_id",
                principalTable: "tenant_refresh_tokens",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_tenant_refresh_tokens_revoked_by_tenant_user_id_tenant_users",
                table: "tenant_refresh_tokens",
                column: "revoked_by_tenant_user_id",
                principalTable: "tenant_users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_tenant_refresh_tokens_tenant_id_tenants",
                table: "tenant_refresh_tokens",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_tenant_refresh_tokens_user_id_tenant_users",
                table: "tenant_refresh_tokens",
                column: "user_id",
                principalTable: "tenant_users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_tenant_role_permissions_granted_by",
                table: "tenant_role_permissions",
                column: "granted_by_tenant_user_id",
                principalTable: "tenant_users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_tenant_role_permissions_permission_id_permission_definitions",
                table: "tenant_role_permissions",
                column: "permission_id",
                principalTable: "permission_definitions",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_tenant_role_permissions_revoked_by",
                table: "tenant_role_permissions",
                column: "revoked_by_tenant_user_id",
                principalTable: "tenant_users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_tenant_role_permissions_role_id_tenant_roles",
                table: "tenant_role_permissions",
                column: "role_id",
                principalTable: "tenant_roles",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_tenant_role_permissions_tenant_id_tenants",
                table: "tenant_role_permissions",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_tenant_roles_created_by",
                table: "tenant_roles",
                column: "created_by_tenant_user_id",
                principalTable: "tenant_users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_tenant_roles_source_role_template_id_role_templates",
                table: "tenant_roles",
                column: "source_role_template_id",
                principalTable: "role_templates",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_tenant_roles_source_role_template_version_id_role_template_versions",
                table: "tenant_roles",
                column: "source_role_template_version_id",
                principalTable: "role_template_versions",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_tenant_roles_updated_by",
                table: "tenant_roles",
                column: "updated_by_tenant_user_id",
                principalTable: "tenant_users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_tenant_user_permissions_assigned_by",
                table: "tenant_user_permissions",
                column: "assigned_by_tenant_user_id",
                principalTable: "tenant_users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_tenant_user_permissions_permission_id_permission_definitions",
                table: "tenant_user_permissions",
                column: "permission_id",
                principalTable: "permission_definitions",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_tenant_user_permissions_tenant_id_tenants",
                table: "tenant_user_permissions",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_tenant_user_permissions_user_id_tenant_users",
                table: "tenant_user_permissions",
                column: "user_id",
                principalTable: "tenant_users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_tenant_user_roles_assigned_by",
                table: "tenant_user_roles",
                column: "assigned_by_tenant_user_id",
                principalTable: "tenant_users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_tenant_user_roles_role_id_tenant_roles",
                table: "tenant_user_roles",
                column: "role_id",
                principalTable: "tenant_roles",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_tenant_user_roles_tenant_id_tenants",
                table: "tenant_user_roles",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_tenant_user_roles_user_id_tenant_users",
                table: "tenant_user_roles",
                column: "user_id",
                principalTable: "tenant_users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_tenant_users_created_by",
                table: "tenant_users",
                column: "created_by_tenant_user_id",
                principalTable: "tenant_users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "fk_tenant_users_updated_by",
                table: "tenant_users",
                column: "updated_by_tenant_user_id",
                principalTable: "tenant_users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "fk_tenants_base_currency_code_currencies",
                table: "tenants",
                column: "base_currency_code",
                principalTable: "currencies",
                principalColumn: "currency_code",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_user_invites_accepted_tenant_user_id_tenant_users",
                table: "user_invites",
                column: "accepted_tenant_user_id",
                principalTable: "tenant_users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_user_invites_initial_role_id_tenant_roles",
                table: "user_invites",
                column: "initial_role_id",
                principalTable: "tenant_roles",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_user_setup_tokens_tenant_id_tenants",
                table: "user_setup_tokens",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_user_setup_tokens_user_id_tenant_users",
                table: "user_setup_tokens",
                column: "user_id",
                principalTable: "tenant_users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_email_verification_tokens_tenant_id_tenants",
                table: "email_verification_tokens");

            migrationBuilder.DropForeignKey(
                name: "fk_email_verification_tokens_user_id_tenant_users",
                table: "email_verification_tokens");

            migrationBuilder.DropForeignKey(
                name: "fk_outlet_user_permissions_assigned_by",
                table: "outlet_user_permissions");

            migrationBuilder.DropForeignKey(
                name: "fk_outlet_user_permissions_permission_id_permission_definitions",
                table: "outlet_user_permissions");

            migrationBuilder.DropForeignKey(
                name: "fk_outlet_user_permissions_revoked_by",
                table: "outlet_user_permissions");

            migrationBuilder.DropForeignKey(
                name: "fk_outlet_user_permissions_tenant_id_tenants",
                table: "outlet_user_permissions");

            migrationBuilder.DropForeignKey(
                name: "fk_outlet_user_permissions_user_id_tenant_users",
                table: "outlet_user_permissions");

            migrationBuilder.DropForeignKey(
                name: "fk_outlet_user_roles_assigned_by",
                table: "outlet_user_roles");

            migrationBuilder.DropForeignKey(
                name: "fk_outlet_user_roles_revoked_by",
                table: "outlet_user_roles");

            migrationBuilder.DropForeignKey(
                name: "fk_outlet_user_roles_role_id_tenant_roles",
                table: "outlet_user_roles");

            migrationBuilder.DropForeignKey(
                name: "fk_outlet_user_roles_tenant_id_tenants",
                table: "outlet_user_roles");

            migrationBuilder.DropForeignKey(
                name: "fk_outlet_user_roles_user_id_tenant_users",
                table: "outlet_user_roles");

            migrationBuilder.DropForeignKey(
                name: "fk_password_reset_tokens_tenant_id_tenants",
                table: "password_reset_tokens");

            migrationBuilder.DropForeignKey(
                name: "fk_password_reset_tokens_user_id_tenant_users",
                table: "password_reset_tokens");

            migrationBuilder.DropForeignKey(
                name: "fk_role_template_version_permissions_permission_id_permission_definitions",
                table: "role_template_version_permissions");

            migrationBuilder.DropForeignKey(
                name: "fk_role_template_versions_created_by",
                table: "role_template_versions");

            migrationBuilder.DropForeignKey(
                name: "fk_role_templates_created_by",
                table: "role_templates");

            migrationBuilder.DropForeignKey(
                name: "fk_role_templates_updated_by",
                table: "role_templates");

            migrationBuilder.DropForeignKey(
                name: "fk_tenant_auth_sessions_revoked_by_tenant_user_id_tenant_users",
                table: "tenant_auth_sessions");

            migrationBuilder.DropForeignKey(
                name: "fk_tenant_auth_sessions_tenant_id_tenants",
                table: "tenant_auth_sessions");

            migrationBuilder.DropForeignKey(
                name: "fk_tenant_auth_sessions_user_id_tenant_users",
                table: "tenant_auth_sessions");

            migrationBuilder.DropForeignKey(
                name: "fk_tenant_login_audits_auth_session_id_tenant_auth_sessions",
                table: "tenant_login_audits");

            migrationBuilder.DropForeignKey(
                name: "fk_tenant_login_audits_tenant_id_tenants",
                table: "tenant_login_audits");

            migrationBuilder.DropForeignKey(
                name: "fk_tenant_login_audits_user_id_tenant_users",
                table: "tenant_login_audits");

            migrationBuilder.DropForeignKey(
                name: "fk_tenant_profiles_tenant_id_tenants",
                table: "tenant_profiles");

            migrationBuilder.DropForeignKey(
                name: "fk_tenant_refresh_tokens_replaced_by_token_id_tenant_refresh_tokens",
                table: "tenant_refresh_tokens");

            migrationBuilder.DropForeignKey(
                name: "fk_tenant_refresh_tokens_revoked_by_tenant_user_id_tenant_users",
                table: "tenant_refresh_tokens");

            migrationBuilder.DropForeignKey(
                name: "fk_tenant_refresh_tokens_tenant_id_tenants",
                table: "tenant_refresh_tokens");

            migrationBuilder.DropForeignKey(
                name: "fk_tenant_refresh_tokens_user_id_tenant_users",
                table: "tenant_refresh_tokens");

            migrationBuilder.DropForeignKey(
                name: "fk_tenant_role_permissions_granted_by",
                table: "tenant_role_permissions");

            migrationBuilder.DropForeignKey(
                name: "fk_tenant_role_permissions_permission_id_permission_definitions",
                table: "tenant_role_permissions");

            migrationBuilder.DropForeignKey(
                name: "fk_tenant_role_permissions_revoked_by",
                table: "tenant_role_permissions");

            migrationBuilder.DropForeignKey(
                name: "fk_tenant_role_permissions_role_id_tenant_roles",
                table: "tenant_role_permissions");

            migrationBuilder.DropForeignKey(
                name: "fk_tenant_role_permissions_tenant_id_tenants",
                table: "tenant_role_permissions");

            migrationBuilder.DropForeignKey(
                name: "fk_tenant_roles_created_by",
                table: "tenant_roles");

            migrationBuilder.DropForeignKey(
                name: "fk_tenant_roles_source_role_template_id_role_templates",
                table: "tenant_roles");

            migrationBuilder.DropForeignKey(
                name: "fk_tenant_roles_source_role_template_version_id_role_template_versions",
                table: "tenant_roles");

            migrationBuilder.DropForeignKey(
                name: "fk_tenant_roles_updated_by",
                table: "tenant_roles");

            migrationBuilder.DropForeignKey(
                name: "fk_tenant_user_permissions_assigned_by",
                table: "tenant_user_permissions");

            migrationBuilder.DropForeignKey(
                name: "fk_tenant_user_permissions_permission_id_permission_definitions",
                table: "tenant_user_permissions");

            migrationBuilder.DropForeignKey(
                name: "fk_tenant_user_permissions_tenant_id_tenants",
                table: "tenant_user_permissions");

            migrationBuilder.DropForeignKey(
                name: "fk_tenant_user_permissions_user_id_tenant_users",
                table: "tenant_user_permissions");

            migrationBuilder.DropForeignKey(
                name: "fk_tenant_user_roles_assigned_by",
                table: "tenant_user_roles");

            migrationBuilder.DropForeignKey(
                name: "fk_tenant_user_roles_role_id_tenant_roles",
                table: "tenant_user_roles");

            migrationBuilder.DropForeignKey(
                name: "fk_tenant_user_roles_tenant_id_tenants",
                table: "tenant_user_roles");

            migrationBuilder.DropForeignKey(
                name: "fk_tenant_user_roles_user_id_tenant_users",
                table: "tenant_user_roles");

            migrationBuilder.DropForeignKey(
                name: "fk_tenant_users_created_by",
                table: "tenant_users");

            migrationBuilder.DropForeignKey(
                name: "fk_tenant_users_updated_by",
                table: "tenant_users");

            migrationBuilder.DropForeignKey(
                name: "fk_tenants_base_currency_code_currencies",
                table: "tenants");

            migrationBuilder.DropForeignKey(
                name: "fk_user_invites_accepted_tenant_user_id_tenant_users",
                table: "user_invites");

            migrationBuilder.DropForeignKey(
                name: "fk_user_invites_initial_role_id_tenant_roles",
                table: "user_invites");

            migrationBuilder.DropForeignKey(
                name: "fk_user_setup_tokens_tenant_id_tenants",
                table: "user_setup_tokens");

            migrationBuilder.DropForeignKey(
                name: "fk_user_setup_tokens_user_id_tenant_users",
                table: "user_setup_tokens");

            migrationBuilder.DropIndex(
                name: "IX_user_setup_tokens_tenant_id",
                table: "user_setup_tokens");

            migrationBuilder.DropIndex(
                name: "IX_user_invites_accepted_tenant_user_id",
                table: "user_invites");

            migrationBuilder.DropIndex(
                name: "IX_user_invites_initial_role_id",
                table: "user_invites");

            migrationBuilder.DropIndex(
                name: "uq_user_invites_invite_token_hash",
                table: "user_invites");

            migrationBuilder.DropIndex(
                name: "uq_user_invites_tenant_id_normalized_invited_email",
                table: "user_invites");

            migrationBuilder.DropIndex(
                name: "IX_tenants_base_currency_code",
                table: "tenants");

            migrationBuilder.DropIndex(
                name: "uq_tenants_tenant_slug",
                table: "tenants");

            migrationBuilder.DropIndex(
                name: "IX_tenant_users_created_by_tenant_user_id",
                table: "tenant_users");

            migrationBuilder.DropIndex(
                name: "IX_tenant_users_updated_by_tenant_user_id",
                table: "tenant_users");

            migrationBuilder.DropIndex(
                name: "uq_tenant_users_tenant_id_email",
                table: "tenant_users");

            migrationBuilder.DropIndex(
                name: "uq_tenant_users_tenant_id_unmasked_phone",
                table: "tenant_users");

            migrationBuilder.DropCheckConstraint(
                name: "ck_tenant_users_locked_until",
                table: "tenant_users");

            migrationBuilder.DropCheckConstraint(
                name: "ck_tenant_users_source_user_type",
                table: "tenant_users");

            migrationBuilder.DropIndex(
                name: "IX_tenant_user_roles_assigned_by_tenant_user_id",
                table: "tenant_user_roles");

            migrationBuilder.DropIndex(
                name: "IX_tenant_user_roles_user_id",
                table: "tenant_user_roles");

            migrationBuilder.DropIndex(
                name: "uq_tenant_user_roles_tenant_id_user_id_role_id",
                table: "tenant_user_roles");

            migrationBuilder.DropIndex(
                name: "IX_tenant_user_permissions_assigned_by_tenant_user_id",
                table: "tenant_user_permissions");

            migrationBuilder.DropIndex(
                name: "IX_tenant_user_permissions_user_id",
                table: "tenant_user_permissions");

            migrationBuilder.DropIndex(
                name: "uq_tenant_user_permissions_tenant_id_user_id_permission_id",
                table: "tenant_user_permissions");

            migrationBuilder.DropIndex(
                name: "IX_tenant_roles_created_by_tenant_user_id",
                table: "tenant_roles");

            migrationBuilder.DropIndex(
                name: "IX_tenant_roles_source_role_template_id",
                table: "tenant_roles");

            migrationBuilder.DropIndex(
                name: "IX_tenant_roles_source_role_template_version_id",
                table: "tenant_roles");

            migrationBuilder.DropIndex(
                name: "uq_tenant_roles_tenant_id_role_name",
                table: "tenant_roles");

            migrationBuilder.DropIndex(
                name: "IX_tenant_role_permissions_granted_by_tenant_user_id",
                table: "tenant_role_permissions");

            migrationBuilder.DropIndex(
                name: "IX_tenant_role_permissions_revoked_by_tenant_user_id",
                table: "tenant_role_permissions");

            migrationBuilder.DropIndex(
                name: "IX_tenant_role_permissions_role_id",
                table: "tenant_role_permissions");

            migrationBuilder.DropIndex(
                name: "uq_tenant_role_permissions_tenant_id_role_id_permission_id",
                table: "tenant_role_permissions");

            migrationBuilder.DropIndex(
                name: "IX_tenant_refresh_tokens_revoked_by_tenant_user_id",
                table: "tenant_refresh_tokens");

            migrationBuilder.DropIndex(
                name: "IX_tenant_refresh_tokens_tenant_auth_session_id",
                table: "tenant_refresh_tokens");

            migrationBuilder.DropIndex(
                name: "IX_tenant_refresh_tokens_tenant_id",
                table: "tenant_refresh_tokens");

            migrationBuilder.DropIndex(
                name: "IX_tenant_refresh_tokens_user_id",
                table: "tenant_refresh_tokens");

            migrationBuilder.DropIndex(
                name: "uq_tenant_refresh_tokens_replaced_by_token_id",
                table: "tenant_refresh_tokens");

            migrationBuilder.DropCheckConstraint(
                name: "ck_tenant_refresh_tokens_revoked_by",
                table: "tenant_refresh_tokens");

            migrationBuilder.DropIndex(
                name: "IX_tenant_login_audits_auth_session_id",
                table: "tenant_login_audits");

            migrationBuilder.DropIndex(
                name: "IX_tenant_login_audits_tenant_id",
                table: "tenant_login_audits");

            migrationBuilder.DropIndex(
                name: "IX_tenant_login_audits_user_id",
                table: "tenant_login_audits");

            migrationBuilder.DropIndex(
                name: "uq_tenant_domains_verification_token_hash",
                table: "tenant_domains");

            migrationBuilder.DropIndex(
                name: "IX_tenant_auth_sessions_tenant_id",
                table: "tenant_auth_sessions");

            migrationBuilder.DropIndex(
                name: "IX_tenant_auth_sessions_user_id",
                table: "tenant_auth_sessions");

            migrationBuilder.DropCheckConstraint(
                name: "ck_tenant_auth_sessions_revoked_by",
                table: "tenant_auth_sessions");

            migrationBuilder.DropCheckConstraint(
                name: "ck_sales_channels_channel_mode",
                table: "sales_channels");

            migrationBuilder.DropCheckConstraint(
                name: "ck_sales_channels_channel_type",
                table: "sales_channels");

            migrationBuilder.DropCheckConstraint(
                name: "ck_sales_channels_sort_order",
                table: "sales_channels");

            migrationBuilder.DropIndex(
                name: "IX_role_templates_created_by_tenant_user_id",
                table: "role_templates");

            migrationBuilder.DropIndex(
                name: "IX_role_templates_updated_by_tenant_user_id",
                table: "role_templates");

            migrationBuilder.DropIndex(
                name: "IX_role_template_versions_created_by_tenant_user_id",
                table: "role_template_versions");

            migrationBuilder.DropCheckConstraint(
                name: "ck_role_template_versions_effective_dates",
                table: "role_template_versions");

            migrationBuilder.DropIndex(
                name: "IX_password_reset_tokens_tenant_id",
                table: "password_reset_tokens");

            migrationBuilder.DropIndex(
                name: "IX_password_reset_tokens_user_id",
                table: "password_reset_tokens");

            migrationBuilder.DropIndex(
                name: "IX_outlet_user_roles_assigned_by_tenant_user_id",
                table: "outlet_user_roles");

            migrationBuilder.DropIndex(
                name: "IX_outlet_user_roles_outlet_id",
                table: "outlet_user_roles");

            migrationBuilder.DropIndex(
                name: "IX_outlet_user_roles_revoked_by_tenant_user_id",
                table: "outlet_user_roles");

            migrationBuilder.DropIndex(
                name: "uq_outlet_user_roles_tenant_outlet_user_role",
                table: "outlet_user_roles");

            migrationBuilder.DropIndex(
                name: "IX_outlet_user_permissions_assigned_by_tenant_user_id",
                table: "outlet_user_permissions");

            migrationBuilder.DropIndex(
                name: "IX_outlet_user_permissions_outlet_id",
                table: "outlet_user_permissions");

            migrationBuilder.DropIndex(
                name: "IX_outlet_user_permissions_revoked_by_tenant_user_id",
                table: "outlet_user_permissions");

            migrationBuilder.DropIndex(
                name: "uq_outlet_user_permissions_tenant_outlet_user_permission",
                table: "outlet_user_permissions");

            migrationBuilder.DropIndex(
                name: "IX_email_verification_tokens_tenant_id",
                table: "email_verification_tokens");

            migrationBuilder.DropIndex(
                name: "IX_email_verification_tokens_user_id",
                table: "email_verification_tokens");

            migrationBuilder.DropCheckConstraint(
                name: "ck_currencies_sort_order",
                table: "currencies");

            migrationBuilder.DropIndex(
                name: "ix_business_types_business_code",
                table: "business_types");

            migrationBuilder.DropColumn(
                name: "expires_at",
                table: "user_setup_tokens");

            migrationBuilder.DropColumn(
                name: "purpose",
                table: "user_setup_tokens");

            migrationBuilder.DropColumn(
                name: "revoked_at",
                table: "user_setup_tokens");

            migrationBuilder.DropColumn(
                name: "tenant_id",
                table: "user_setup_tokens");

            migrationBuilder.DropColumn(
                name: "used_at",
                table: "user_setup_tokens");

            migrationBuilder.DropColumn(
                name: "accepted_at",
                table: "user_invites");

            migrationBuilder.DropColumn(
                name: "accepted_tenant_user_id",
                table: "user_invites");

            migrationBuilder.DropColumn(
                name: "cancelled_at",
                table: "user_invites");

            migrationBuilder.DropColumn(
                name: "expires_at",
                table: "user_invites");

            migrationBuilder.DropColumn(
                name: "initial_outlet_id",
                table: "user_invites");

            migrationBuilder.DropColumn(
                name: "initial_role_id",
                table: "user_invites");

            migrationBuilder.DropColumn(
                name: "invited_by_platform_user_id",
                table: "user_invites");

            migrationBuilder.DropColumn(
                name: "invited_by_tenant_user_id",
                table: "user_invites");

            migrationBuilder.DropColumn(
                name: "invited_email",
                table: "user_invites");

            migrationBuilder.DropColumn(
                name: "invited_phone",
                table: "user_invites");

            migrationBuilder.DropColumn(
                name: "last_sent_at",
                table: "user_invites");

            migrationBuilder.DropColumn(
                name: "normalized_invited_email",
                table: "user_invites");

            migrationBuilder.DropColumn(
                name: "normalized_invited_phone",
                table: "user_invites");

            migrationBuilder.DropColumn(
                name: "resend_count",
                table: "user_invites");

            migrationBuilder.DropColumn(
                name: "sent_at",
                table: "user_invites");

            migrationBuilder.DropColumn(
                name: "activated_at",
                table: "tenants");

            migrationBuilder.DropColumn(
                name: "archived_at",
                table: "tenants");

            migrationBuilder.DropColumn(
                name: "created_by_platform_user_id",
                table: "tenants");

            migrationBuilder.DropColumn(
                name: "data_region",
                table: "tenants");

            migrationBuilder.DropColumn(
                name: "suspended_at",
                table: "tenants");

            migrationBuilder.DropColumn(
                name: "tenant_slug",
                table: "tenants");

            migrationBuilder.DropColumn(
                name: "updated_by_platform_user_id",
                table: "tenants");

            migrationBuilder.DropColumn(
                name: "accepted_privacy_terms",
                table: "tenant_users");

            migrationBuilder.DropColumn(
                name: "accepted_terms_version",
                table: "tenant_users");

            migrationBuilder.DropColumn(
                name: "account_status",
                table: "tenant_users");

            migrationBuilder.DropColumn(
                name: "created_by_tenant_user_id",
                table: "tenant_users");

            migrationBuilder.DropColumn(
                name: "default_outlet_id",
                table: "tenant_users");

            migrationBuilder.DropColumn(
                name: "display_name",
                table: "tenant_users");

            migrationBuilder.DropColumn(
                name: "email",
                table: "tenant_users");

            migrationBuilder.DropColumn(
                name: "encrypted_password",
                table: "tenant_users");

            migrationBuilder.DropColumn(
                name: "failed_login_attempts",
                table: "tenant_users");

            migrationBuilder.DropColumn(
                name: "full_name",
                table: "tenant_users");

            migrationBuilder.DropColumn(
                name: "locked_until",
                table: "tenant_users");

            migrationBuilder.DropColumn(
                name: "notes",
                table: "tenant_users");

            migrationBuilder.DropColumn(
                name: "outlet_id",
                table: "tenant_users");

            migrationBuilder.DropColumn(
                name: "password_change_required_at",
                table: "tenant_users");

            migrationBuilder.DropColumn(
                name: "password_salt",
                table: "tenant_users");

            migrationBuilder.DropColumn(
                name: "phone",
                table: "tenant_users");

            migrationBuilder.DropColumn(
                name: "profile_image_url",
                table: "tenant_users");

            migrationBuilder.DropColumn(
                name: "source_user_type",
                table: "tenant_users");

            migrationBuilder.DropColumn(
                name: "unmasked_phone",
                table: "tenant_users");

            migrationBuilder.DropColumn(
                name: "updated_by_tenant_user_id",
                table: "tenant_users");

            migrationBuilder.DropColumn(
                name: "user_type",
                table: "tenant_users");

            migrationBuilder.DropColumn(
                name: "assigned_at",
                table: "tenant_user_roles");

            migrationBuilder.DropColumn(
                name: "assigned_by_tenant_user_id",
                table: "tenant_user_roles");

            migrationBuilder.DropColumn(
                name: "revoked_at",
                table: "tenant_user_roles");

            migrationBuilder.DropColumn(
                name: "tenant_id",
                table: "tenant_user_roles");

            migrationBuilder.DropColumn(
                name: "assigned_at",
                table: "tenant_user_permissions");

            migrationBuilder.DropColumn(
                name: "assigned_by_tenant_user_id",
                table: "tenant_user_permissions");

            migrationBuilder.DropColumn(
                name: "revoked_at",
                table: "tenant_user_permissions");

            migrationBuilder.DropColumn(
                name: "tenant_id",
                table: "tenant_user_permissions");

            migrationBuilder.DropColumn(
                name: "created_by_platform_user_id",
                table: "tenant_settings");

            migrationBuilder.DropColumn(
                name: "setting_value",
                table: "tenant_settings");

            migrationBuilder.DropColumn(
                name: "updated_by_platform_user_id",
                table: "tenant_settings");

            migrationBuilder.DropColumn(
                name: "created_by_tenant_user_id",
                table: "tenant_roles");

            migrationBuilder.DropColumn(
                name: "is_active",
                table: "tenant_roles");

            migrationBuilder.DropColumn(
                name: "is_custom",
                table: "tenant_roles");

            migrationBuilder.DropColumn(
                name: "role_name",
                table: "tenant_roles");

            migrationBuilder.DropColumn(
                name: "source_role_template_id",
                table: "tenant_roles");

            migrationBuilder.DropColumn(
                name: "source_role_template_version_id",
                table: "tenant_roles");

            migrationBuilder.DropColumn(
                name: "granted_at",
                table: "tenant_role_permissions");

            migrationBuilder.DropColumn(
                name: "granted_by_tenant_user_id",
                table: "tenant_role_permissions");

            migrationBuilder.DropColumn(
                name: "revoked_at",
                table: "tenant_role_permissions");

            migrationBuilder.DropColumn(
                name: "revoked_by_tenant_user_id",
                table: "tenant_role_permissions");

            migrationBuilder.DropColumn(
                name: "tenant_id",
                table: "tenant_role_permissions");

            migrationBuilder.DropColumn(
                name: "replaced_by_token_id",
                table: "tenant_refresh_tokens");

            migrationBuilder.DropColumn(
                name: "revoke_reason",
                table: "tenant_refresh_tokens");

            migrationBuilder.DropColumn(
                name: "revoked_at",
                table: "tenant_refresh_tokens");

            migrationBuilder.DropColumn(
                name: "revoked_by_platform_user_id",
                table: "tenant_refresh_tokens");

            migrationBuilder.DropColumn(
                name: "revoked_by_tenant_user_id",
                table: "tenant_refresh_tokens");

            migrationBuilder.DropColumn(
                name: "tenant_id",
                table: "tenant_refresh_tokens");

            migrationBuilder.DropColumn(
                name: "token_family_id",
                table: "tenant_refresh_tokens");

            migrationBuilder.DropColumn(
                name: "used_at",
                table: "tenant_refresh_tokens");

            migrationBuilder.DropColumn(
                name: "user_id",
                table: "tenant_refresh_tokens");

            migrationBuilder.DropColumn(
                name: "business_type_id",
                table: "tenant_profiles");

            migrationBuilder.DropColumn(
                name: "created_by_platform_user_id",
                table: "tenant_profiles");

            migrationBuilder.DropColumn(
                name: "description",
                table: "tenant_profiles");

            migrationBuilder.DropColumn(
                name: "logo_url",
                table: "tenant_profiles");

            migrationBuilder.DropColumn(
                name: "trading_name",
                table: "tenant_profiles");

            migrationBuilder.DropColumn(
                name: "updated_by_platform_user_id",
                table: "tenant_profiles");

            migrationBuilder.DropColumn(
                name: "attempted_at",
                table: "tenant_login_audits");

            migrationBuilder.DropColumn(
                name: "auth_session_id",
                table: "tenant_login_audits");

            migrationBuilder.DropColumn(
                name: "authentication_method",
                table: "tenant_login_audits");

            migrationBuilder.DropColumn(
                name: "failure_code",
                table: "tenant_login_audits");

            migrationBuilder.DropColumn(
                name: "failure_detail",
                table: "tenant_login_audits");

            migrationBuilder.DropColumn(
                name: "ip_address",
                table: "tenant_login_audits");

            migrationBuilder.DropColumn(
                name: "login_status",
                table: "tenant_login_audits");

            migrationBuilder.DropColumn(
                name: "pos_device_id",
                table: "tenant_login_audits");

            migrationBuilder.DropColumn(
                name: "tenant_id",
                table: "tenant_login_audits");

            migrationBuilder.DropColumn(
                name: "user_agent",
                table: "tenant_login_audits");

            migrationBuilder.DropColumn(
                name: "created_by_platform_user_id",
                table: "tenant_domains");

            migrationBuilder.DropColumn(
                name: "domain_type",
                table: "tenant_domains");

            migrationBuilder.DropColumn(
                name: "is_primary",
                table: "tenant_domains");

            migrationBuilder.DropColumn(
                name: "ssl_expires_at",
                table: "tenant_domains");

            migrationBuilder.DropColumn(
                name: "ssl_issued_at",
                table: "tenant_domains");

            migrationBuilder.DropColumn(
                name: "ssl_status",
                table: "tenant_domains");

            migrationBuilder.DropColumn(
                name: "status",
                table: "tenant_domains");

            migrationBuilder.DropColumn(
                name: "updated_by_platform_user_id",
                table: "tenant_domains");

            migrationBuilder.DropColumn(
                name: "verification_status",
                table: "tenant_domains");

            migrationBuilder.DropColumn(
                name: "verification_token_hash",
                table: "tenant_domains");

            migrationBuilder.DropColumn(
                name: "verified_at",
                table: "tenant_domains");

            migrationBuilder.DropColumn(
                name: "device_name",
                table: "tenant_auth_sessions");

            migrationBuilder.DropColumn(
                name: "expires_at",
                table: "tenant_auth_sessions");

            migrationBuilder.DropColumn(
                name: "ip_address",
                table: "tenant_auth_sessions");

            migrationBuilder.DropColumn(
                name: "last_seen_at",
                table: "tenant_auth_sessions");

            migrationBuilder.DropColumn(
                name: "pos_device_id",
                table: "tenant_auth_sessions");

            migrationBuilder.DropColumn(
                name: "revoke_reason",
                table: "tenant_auth_sessions");

            migrationBuilder.DropColumn(
                name: "revoked_at",
                table: "tenant_auth_sessions");

            migrationBuilder.DropColumn(
                name: "revoked_by_platform_user_id",
                table: "tenant_auth_sessions");

            migrationBuilder.DropColumn(
                name: "tenant_id",
                table: "tenant_auth_sessions");

            migrationBuilder.DropColumn(
                name: "user_agent",
                table: "tenant_auth_sessions");

            migrationBuilder.DropColumn(
                name: "user_id",
                table: "tenant_auth_sessions");

            migrationBuilder.DropColumn(
                name: "address_line1",
                table: "tenant_addresses");

            migrationBuilder.DropColumn(
                name: "address_line2",
                table: "tenant_addresses");

            migrationBuilder.DropColumn(
                name: "created_by_platform_user_id",
                table: "tenant_addresses");

            migrationBuilder.DropColumn(
                name: "is_primary",
                table: "tenant_addresses");

            migrationBuilder.DropColumn(
                name: "status",
                table: "tenant_addresses");

            migrationBuilder.DropColumn(
                name: "updated_by_platform_user_id",
                table: "tenant_addresses");

            migrationBuilder.DropColumn(
                name: "default_value",
                table: "setting_definitions");

            migrationBuilder.DropColumn(
                name: "description",
                table: "setting_definitions");

            migrationBuilder.DropColumn(
                name: "display_name",
                table: "setting_definitions");

            migrationBuilder.DropColumn(
                name: "is_tenant_editable",
                table: "setting_definitions");

            migrationBuilder.DropColumn(
                name: "status",
                table: "setting_definitions");

            migrationBuilder.DropColumn(
                name: "channel_mode",
                table: "sales_channels");

            migrationBuilder.DropColumn(
                name: "channel_name",
                table: "sales_channels");

            migrationBuilder.DropColumn(
                name: "created_by_tenant_user_id",
                table: "role_templates");

            migrationBuilder.DropColumn(
                name: "is_active",
                table: "role_templates");

            migrationBuilder.DropColumn(
                name: "is_default",
                table: "role_templates");

            migrationBuilder.DropColumn(
                name: "template_name",
                table: "role_templates");

            migrationBuilder.DropColumn(
                name: "updated_by_tenant_user_id",
                table: "role_templates");

            migrationBuilder.DropColumn(
                name: "created_by_tenant_user_id",
                table: "role_template_versions");

            migrationBuilder.DropColumn(
                name: "effective_from",
                table: "role_template_versions");

            migrationBuilder.DropColumn(
                name: "effective_until",
                table: "role_template_versions");

            migrationBuilder.DropColumn(
                name: "is_active",
                table: "role_template_versions");

            migrationBuilder.DropColumn(
                name: "version_label",
                table: "role_template_versions");

            migrationBuilder.DropColumn(
                name: "is_active",
                table: "role_template_version_permissions");

            migrationBuilder.DropColumn(
                name: "action_type",
                table: "permission_definitions");

            migrationBuilder.DropColumn(
                name: "description",
                table: "permission_definitions");

            migrationBuilder.DropColumn(
                name: "feature_id",
                table: "permission_definitions");

            migrationBuilder.DropColumn(
                name: "is_active",
                table: "permission_definitions");

            migrationBuilder.DropColumn(
                name: "is_system",
                table: "permission_definitions");

            migrationBuilder.DropColumn(
                name: "module_id",
                table: "permission_definitions");

            migrationBuilder.DropColumn(
                name: "expires_at",
                table: "password_reset_tokens");

            migrationBuilder.DropColumn(
                name: "requested_ip_address",
                table: "password_reset_tokens");

            migrationBuilder.DropColumn(
                name: "requested_user_agent",
                table: "password_reset_tokens");

            migrationBuilder.DropColumn(
                name: "revoked_at",
                table: "password_reset_tokens");

            migrationBuilder.DropColumn(
                name: "tenant_id",
                table: "password_reset_tokens");

            migrationBuilder.DropColumn(
                name: "used_at",
                table: "password_reset_tokens");

            migrationBuilder.DropColumn(
                name: "user_id",
                table: "password_reset_tokens");

            migrationBuilder.DropColumn(
                name: "assigned_at",
                table: "outlet_user_roles");

            migrationBuilder.DropColumn(
                name: "assigned_by_tenant_user_id",
                table: "outlet_user_roles");

            migrationBuilder.DropColumn(
                name: "revoked_at",
                table: "outlet_user_roles");

            migrationBuilder.DropColumn(
                name: "revoked_by_tenant_user_id",
                table: "outlet_user_roles");

            migrationBuilder.DropColumn(
                name: "tenant_id",
                table: "outlet_user_roles");

            migrationBuilder.DropColumn(
                name: "assigned_at",
                table: "outlet_user_permissions");

            migrationBuilder.DropColumn(
                name: "assigned_by_tenant_user_id",
                table: "outlet_user_permissions");

            migrationBuilder.DropColumn(
                name: "revoked_at",
                table: "outlet_user_permissions");

            migrationBuilder.DropColumn(
                name: "revoked_by_tenant_user_id",
                table: "outlet_user_permissions");

            migrationBuilder.DropColumn(
                name: "tenant_id",
                table: "outlet_user_permissions");

            migrationBuilder.DropColumn(
                name: "email_to_verify",
                table: "email_verification_tokens");

            migrationBuilder.DropColumn(
                name: "expires_at",
                table: "email_verification_tokens");

            migrationBuilder.DropColumn(
                name: "normalized_email_to_verify",
                table: "email_verification_tokens");

            migrationBuilder.DropColumn(
                name: "revoked_at",
                table: "email_verification_tokens");

            migrationBuilder.DropColumn(
                name: "tenant_id",
                table: "email_verification_tokens");

            migrationBuilder.DropColumn(
                name: "user_id",
                table: "email_verification_tokens");

            migrationBuilder.DropColumn(
                name: "verified_at",
                table: "email_verification_tokens");

            migrationBuilder.DropColumn(
                name: "currency_name",
                table: "currencies");

            migrationBuilder.DropColumn(
                name: "currency_symbol",
                table: "currencies");

            migrationBuilder.DropColumn(
                name: "is_active",
                table: "currencies");

            migrationBuilder.DropColumn(
                name: "sort_order",
                table: "currencies");

            migrationBuilder.DropColumn(
                name: "business_code",
                table: "business_types");

            migrationBuilder.RenameColumn(
                name: "user_id",
                table: "user_setup_tokens",
                newName: "user_invite_id");

            migrationBuilder.RenameIndex(
                name: "IX_user_setup_tokens_user_id",
                table: "user_setup_tokens",
                newName: "IX_user_setup_tokens_user_invite_id");

            migrationBuilder.RenameColumn(
                name: "display_name",
                table: "tenants",
                newName: "name");

            migrationBuilder.RenameColumn(
                name: "base_currency_code",
                table: "tenants",
                newName: "currency_code");

            migrationBuilder.RenameColumn(
                name: "user_id",
                table: "tenant_user_roles",
                newName: "tenant_user_id");

            migrationBuilder.RenameColumn(
                name: "role_id",
                table: "tenant_user_roles",
                newName: "tenant_role_id");

            migrationBuilder.RenameIndex(
                name: "IX_tenant_user_roles_role_id",
                table: "tenant_user_roles",
                newName: "IX_tenant_user_roles_tenant_role_id");

            migrationBuilder.RenameColumn(
                name: "user_id",
                table: "tenant_user_permissions",
                newName: "tenant_user_id");

            migrationBuilder.RenameColumn(
                name: "permission_id",
                table: "tenant_user_permissions",
                newName: "permission_definition_id");

            migrationBuilder.RenameIndex(
                name: "IX_tenant_user_permissions_permission_id",
                table: "tenant_user_permissions",
                newName: "IX_tenant_user_permissions_permission_definition_id");

            migrationBuilder.RenameColumn(
                name: "updated_by_tenant_user_id",
                table: "tenant_roles",
                newName: "role_template_version_id");

            migrationBuilder.RenameColumn(
                name: "role_description",
                table: "tenant_roles",
                newName: "description");

            migrationBuilder.RenameIndex(
                name: "IX_tenant_roles_updated_by_tenant_user_id",
                table: "tenant_roles",
                newName: "IX_tenant_roles_role_template_version_id");

            migrationBuilder.RenameColumn(
                name: "role_id",
                table: "tenant_role_permissions",
                newName: "tenant_role_id");

            migrationBuilder.RenameColumn(
                name: "permission_id",
                table: "tenant_role_permissions",
                newName: "permission_definition_id");

            migrationBuilder.RenameColumn(
                name: "notes",
                table: "tenant_role_permissions",
                newName: "description");

            migrationBuilder.RenameIndex(
                name: "IX_tenant_role_permissions_permission_id",
                table: "tenant_role_permissions",
                newName: "IX_tenant_role_permissions_permission_definition_id");

            migrationBuilder.RenameColumn(
                name: "user_id",
                table: "tenant_login_audits",
                newName: "tenant_user_id");

            migrationBuilder.RenameColumn(
                name: "attempted_identifier",
                table: "tenant_login_audits",
                newName: "login_result");

            migrationBuilder.RenameColumn(
                name: "revoked_by_tenant_user_id",
                table: "tenant_auth_sessions",
                newName: "tenant_user_id");

            migrationBuilder.RenameIndex(
                name: "IX_tenant_auth_sessions_revoked_by_tenant_user_id",
                table: "tenant_auth_sessions",
                newName: "IX_tenant_auth_sessions_tenant_user_id");

            migrationBuilder.RenameColumn(
                name: "state_or_province",
                table: "tenant_addresses",
                newName: "state");

            migrationBuilder.RenameIndex(
                name: "ix_sales_channels_tenant_id_channel_code",
                table: "sales_channels",
                newName: "uq_sales_channels_tenant_id_channel_code");

            migrationBuilder.RenameColumn(
                name: "permission_id",
                table: "role_template_version_permissions",
                newName: "permission_definition_id");

            // migrationBuilder.RenameIndex(
            //     name: "uq_role_template_version_permissions_role_template_version_id_permission_id",
            //     table: "role_template_version_permissions",
            //     newName: "uq_role_template_version_permissions_role_template_version_id_permission_definition_id");

            migrationBuilder.RenameIndex(
                name: "IX_role_template_version_permissions_permission_id",
                table: "role_template_version_permissions",
                newName: "IX_role_template_version_permissions_permission_definition_id");

            migrationBuilder.RenameColumn(
                name: "user_id",
                table: "outlet_user_roles",
                newName: "tenant_user_id");

            migrationBuilder.RenameColumn(
                name: "role_id",
                table: "outlet_user_roles",
                newName: "tenant_role_id");

            migrationBuilder.RenameIndex(
                name: "IX_outlet_user_roles_user_id",
                table: "outlet_user_roles",
                newName: "IX_outlet_user_roles_tenant_user_id");

            migrationBuilder.RenameIndex(
                name: "IX_outlet_user_roles_role_id",
                table: "outlet_user_roles",
                newName: "IX_outlet_user_roles_tenant_role_id");

            migrationBuilder.RenameColumn(
                name: "user_id",
                table: "outlet_user_permissions",
                newName: "tenant_user_id");

            migrationBuilder.RenameColumn(
                name: "permission_id",
                table: "outlet_user_permissions",
                newName: "permission_definition_id");

            migrationBuilder.RenameIndex(
                name: "IX_outlet_user_permissions_user_id",
                table: "outlet_user_permissions",
                newName: "IX_outlet_user_permissions_tenant_user_id");

            migrationBuilder.RenameIndex(
                name: "IX_outlet_user_permissions_permission_id",
                table: "outlet_user_permissions",
                newName: "IX_outlet_user_permissions_permission_definition_id");

            migrationBuilder.RenameColumn(
                name: "business_name",
                table: "business_types",
                newName: "business_type_name");

            migrationBuilder.AddColumn<string>(
                name: "status",
                table: "user_setup_tokens",
                type: "varchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<string>(
                name: "invite_status",
                table: "user_invites",
                type: "varchar(30)",
                maxLength: 30,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(40)",
                oldMaxLength: 40);

            migrationBuilder.AddColumn<Guid>(
                name: "tenant_user_id",
                table: "user_invites",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AlterColumn<string>(
                name: "status",
                table: "tenants",
                type: "varchar(30)",
                maxLength: 30,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(40)",
                oldMaxLength: 40);

            migrationBuilder.AlterColumn<string>(
                name: "default_timezone",
                table: "tenants",
                type: "varchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(80)",
                oldMaxLength: 80);

            migrationBuilder.AddColumn<string>(
                name: "base_currency",
                table: "tenants",
                type: "char(3)",
                maxLength: 3,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "billing_status",
                table: "tenants",
                type: "varchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "business_type",
                table: "tenants",
                type: "varchar(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "business_type_id",
                table: "tenants",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "default_locale",
                table: "tenants",
                type: "varchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "operating_mode",
                table: "tenants",
                type: "varchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "primary_domain",
                table: "tenants",
                type: "varchar(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "");

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

            migrationBuilder.AddColumn<string>(
                name: "normalized_email",
                table: "tenant_users",
                type: "citext",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "normalized_phone",
                table: "tenant_users",
                type: "varchar(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "password_hash",
                table: "tenant_users",
                type: "varchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "status",
                table: "tenant_users",
                type: "varchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<Guid>(
                name: "tenant_user_id",
                table: "tenant_user_roles",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<string>(
                name: "description",
                table: "tenant_user_roles",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "updated_at",
                table: "tenant_user_roles",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AlterColumn<Guid>(
                name: "tenant_user_id",
                table: "tenant_user_permissions",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<string>(
                name: "description",
                table: "tenant_user_permissions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "updated_at",
                table: "tenant_user_permissions",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AlterColumn<string>(
                name: "role_code",
                table: "tenant_roles",
                type: "varchar(80)",
                maxLength: 80,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(100)",
                oldMaxLength: 100);

            migrationBuilder.AddColumn<string>(
                name: "name",
                table: "tenant_roles",
                type: "varchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "status",
                table: "tenant_roles",
                type: "varchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "updated_at",
                table: "tenant_role_permissions",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<string>(
                name: "status",
                table: "tenant_refresh_tokens",
                type: "varchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<string>(
                name: "website_url",
                table: "tenant_profiles",
                type: "varchar(255)",
                maxLength: 255,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "legal_name",
                table: "tenant_profiles",
                type: "varchar(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(250)",
                oldMaxLength: 250);

            migrationBuilder.AddColumn<string>(
                name: "country_code",
                table: "tenant_profiles",
                type: "char(2)",
                maxLength: 2,
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

            migrationBuilder.AlterColumn<string>(
                name: "domain_name",
                table: "tenant_domains",
                type: "varchar(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(255)",
                oldMaxLength: 255);

            migrationBuilder.AddColumn<string>(
                name: "domain_status",
                table: "tenant_domains",
                type: "varchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "session_token_hash",
                table: "tenant_auth_sessions",
                type: "varchar(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "status",
                table: "tenant_auth_sessions",
                type: "varchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<string>(
                name: "country_code",
                table: "tenant_addresses",
                type: "char(2)",
                maxLength: 2,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "char(2)",
                oldMaxLength: 2);

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

            migrationBuilder.AlterColumn<string>(
                name: "value_type",
                table: "setting_definitions",
                type: "varchar(40)",
                maxLength: 40,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(30)",
                oldMaxLength: 30);

            migrationBuilder.AlterColumn<string>(
                name: "setting_key",
                table: "setting_definitions",
                type: "varchar(255)",
                maxLength: 255,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(120)",
                oldMaxLength: 120);

            migrationBuilder.AlterColumn<string>(
                name: "status",
                table: "sales_channels",
                type: "varchar(30)",
                maxLength: 30,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(40)",
                oldMaxLength: 40);

            migrationBuilder.AddColumn<string>(
                name: "name",
                table: "sales_channels",
                type: "varchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<string>(
                name: "template_code",
                table: "role_templates",
                type: "varchar(80)",
                maxLength: 80,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(100)",
                oldMaxLength: 100);

            migrationBuilder.AddColumn<string>(
                name: "name",
                table: "role_templates",
                type: "varchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "sort_order",
                table: "role_templates",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "status",
                table: "role_templates",
                type: "varchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "description",
                table: "role_template_version_permissions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "updated_at",
                table: "role_template_version_permissions",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AlterColumn<string>(
                name: "permission_code",
                table: "permission_definitions",
                type: "varchar(80)",
                maxLength: 80,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(100)",
                oldMaxLength: 100);

            migrationBuilder.AddColumn<string>(
                name: "name",
                table: "permission_definitions",
                type: "varchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "status",
                table: "permission_definitions",
                type: "varchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "status",
                table: "password_reset_tokens",
                type: "varchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "tenant_user_id",
                table: "password_reset_tokens",
                type: "uuid",
                nullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "outlet_id",
                table: "outlet_user_roles",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<Guid>(
                name: "tenant_user_id",
                table: "outlet_user_roles",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<string>(
                name: "description",
                table: "outlet_user_roles",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "updated_at",
                table: "outlet_user_roles",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AlterColumn<Guid>(
                name: "outlet_id",
                table: "outlet_user_permissions",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<Guid>(
                name: "tenant_user_id",
                table: "outlet_user_permissions",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<string>(
                name: "description",
                table: "outlet_user_permissions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "updated_at",
                table: "outlet_user_permissions",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<string>(
                name: "status",
                table: "email_verification_tokens",
                type: "varchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "tenant_user_id",
                table: "email_verification_tokens",
                type: "uuid",
                nullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "decimal_places",
                table: "currencies",
                type: "integer",
                nullable: false,
                oldClrType: typeof(short),
                oldType: "smallint");

            migrationBuilder.AddColumn<string>(
                name: "name",
                table: "currencies",
                type: "varchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "business_type_key",
                table: "business_types",
                type: "varchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "is_system_type",
                table: "business_types",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<int>(
                name: "sort_order",
                table: "business_types",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddCheckConstraint(
                name: "ck_user_setup_tokens_status",
                table: "user_setup_tokens",
                sql: "status IN ('PENDING', 'USED', 'EXPIRED', 'REVOKED')");

            migrationBuilder.CreateIndex(
                name: "IX_user_invites_tenant_user_id",
                table: "user_invites",
                column: "tenant_user_id");

            migrationBuilder.CreateIndex(
                name: "uq_user_invites_tenant_id_invite_token_hash",
                table: "user_invites",
                columns: new[] { "tenant_id", "invite_token_hash" },
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_user_invites_invite_status",
                table: "user_invites",
                sql: "invite_status IN ('PENDING', 'ACCEPTED', 'EXPIRED', 'REVOKED')");

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
                name: "uq_tenant_users_normalized_email",
                table: "tenant_users",
                column: "normalized_email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_tenant_users_tenant_id_normalized_phone",
                table: "tenant_users",
                columns: new[] { "tenant_id", "normalized_phone" },
                unique: true,
                filter: "normalized_phone IS NOT NULL");

            migrationBuilder.AddCheckConstraint(
                name: "ck_tenant_users_status",
                table: "tenant_users",
                sql: "status IN ('ACTIVE', 'INACTIVE', 'INVITED', 'LOCKED', 'DELETED')");

            migrationBuilder.CreateIndex(
                name: "uq_tenant_user_roles_tenant_user_id_tenant_role_id",
                table: "tenant_user_roles",
                columns: new[] { "tenant_user_id", "tenant_role_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_tenant_user_permissions_tenant_user_id_permission_definition_id",
                table: "tenant_user_permissions",
                columns: new[] { "tenant_user_id", "permission_definition_id" },
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_tenant_roles_status",
                table: "tenant_roles",
                sql: "status IN ('ACTIVE', 'INACTIVE', 'DELETED')");

            migrationBuilder.CreateIndex(
                name: "uq_tenant_role_permissions_tenant_role_id_permission_definition_id",
                table: "tenant_role_permissions",
                columns: new[] { "tenant_role_id", "permission_definition_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_tenant_refresh_tokens_tenant_auth_session_id_status",
                table: "tenant_refresh_tokens",
                columns: new[] { "tenant_auth_session_id", "status" });

            migrationBuilder.AddCheckConstraint(
                name: "ck_tenant_refresh_tokens_expires_at_created_at",
                table: "tenant_refresh_tokens",
                sql: "expires_at > created_at");

            migrationBuilder.AddCheckConstraint(
                name: "ck_tenant_refresh_tokens_status",
                table: "tenant_refresh_tokens",
                sql: "status IN ('ACTIVE', 'USED', 'EXPIRED', 'REVOKED')");

            migrationBuilder.CreateIndex(
                name: "ix_tenant_login_audits_tenant_user_id_login_result_created_at",
                table: "tenant_login_audits",
                columns: new[] { "tenant_user_id", "login_result", "created_at" });

            migrationBuilder.AddCheckConstraint(
                name: "ck_tenant_login_audits_login_result",
                table: "tenant_login_audits",
                sql: "login_result IN ('SUCCESS', 'FAILED', 'LOCKED')");

            migrationBuilder.AddCheckConstraint(
                name: "ck_tenant_domains_domain_status",
                table: "tenant_domains",
                sql: "domain_status IN ('PENDING', 'VERIFIED', 'FAILED', 'DISABLED')");

            migrationBuilder.CreateIndex(
                name: "uq_tenant_auth_sessions_session_token_hash",
                table: "tenant_auth_sessions",
                column: "session_token_hash",
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_tenant_auth_sessions_status",
                table: "tenant_auth_sessions",
                sql: "status IN ('ACTIVE', 'EXPIRED', 'REVOKED')");

            migrationBuilder.AddCheckConstraint(
                name: "ck_setting_definitions_value_type",
                table: "setting_definitions",
                sql: "value_type IN ('STRING', 'NUMBER', 'BOOLEAN', 'JSON', 'DATE')");

            migrationBuilder.CreateIndex(
                name: "uq_sales_channels_tenant_id_id",
                table: "sales_channels",
                columns: new[] { "tenant_id", "id" },
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_sales_channels_channel_type",
                table: "sales_channels",
                sql: "channel_type IN ('E_POS', 'E_COMMERCE')");

            migrationBuilder.AddCheckConstraint(
                name: "ck_role_templates_status",
                table: "role_templates",
                sql: "status IN ('ACTIVE', 'INACTIVE', 'DELETED')");

            migrationBuilder.AddCheckConstraint(
                name: "ck_permission_definitions_status",
                table: "permission_definitions",
                sql: "status IN ('ACTIVE', 'INACTIVE', 'DELETED')");

            migrationBuilder.CreateIndex(
                name: "IX_password_reset_tokens_tenant_user_id",
                table: "password_reset_tokens",
                column: "tenant_user_id");

            migrationBuilder.AddCheckConstraint(
                name: "ck_password_reset_tokens_status",
                table: "password_reset_tokens",
                sql: "status IN ('PENDING', 'USED', 'EXPIRED', 'REVOKED')");

            migrationBuilder.CreateIndex(
                name: "uq_outlet_user_roles_outlet_id_tenant_user_id_tenant_role_id",
                table: "outlet_user_roles",
                columns: new[] { "outlet_id", "tenant_user_id", "tenant_role_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_outlet_user_permissions_outlet_id_tenant_user_id_permission_definition_id",
                table: "outlet_user_permissions",
                columns: new[] { "outlet_id", "tenant_user_id", "permission_definition_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_email_verification_tokens_tenant_user_id",
                table: "email_verification_tokens",
                column: "tenant_user_id");

            migrationBuilder.AddCheckConstraint(
                name: "ck_email_verification_tokens_status",
                table: "email_verification_tokens",
                sql: "status IN ('PENDING', 'VERIFIED', 'EXPIRED', 'REVOKED')");

            migrationBuilder.CreateIndex(
                name: "uq_business_types_business_type_key",
                table: "business_types",
                column: "business_type_key",
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_business_types_sort_order",
                table: "business_types",
                sql: "sort_order >= 0");

            migrationBuilder.AddForeignKey(
                name: "fk_email_verification_tokens_tenant_user_id_tenant_users",
                table: "email_verification_tokens",
                column: "tenant_user_id",
                principalTable: "tenant_users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_outlet_user_permissions_permission_definition_id_permission_definitions",
                table: "outlet_user_permissions",
                column: "permission_definition_id",
                principalTable: "permission_definitions",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_outlet_user_permissions_tenant_user_id_tenant_users",
                table: "outlet_user_permissions",
                column: "tenant_user_id",
                principalTable: "tenant_users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_outlet_user_roles_tenant_role_id_tenant_roles",
                table: "outlet_user_roles",
                column: "tenant_role_id",
                principalTable: "tenant_roles",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_outlet_user_roles_tenant_user_id_tenant_users",
                table: "outlet_user_roles",
                column: "tenant_user_id",
                principalTable: "tenant_users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_password_reset_tokens_tenant_user_id_tenant_users",
                table: "password_reset_tokens",
                column: "tenant_user_id",
                principalTable: "tenant_users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_role_template_version_permissions_permission_definition_id_permission_definitions",
                table: "role_template_version_permissions",
                column: "permission_definition_id",
                principalTable: "permission_definitions",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_tenant_auth_sessions_tenant_user_id_tenant_users",
                table: "tenant_auth_sessions",
                column: "tenant_user_id",
                principalTable: "tenant_users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_tenant_login_audits_tenant_user_id_tenant_users",
                table: "tenant_login_audits",
                column: "tenant_user_id",
                principalTable: "tenant_users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_tenant_profiles_tenant_id_tenants",
                table: "tenant_profiles",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_tenant_role_permissions_permission_definition_id_permission_definitions",
                table: "tenant_role_permissions",
                column: "permission_definition_id",
                principalTable: "permission_definitions",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_tenant_role_permissions_tenant_role_id_tenant_roles",
                table: "tenant_role_permissions",
                column: "tenant_role_id",
                principalTable: "tenant_roles",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_tenant_roles_role_template_version_id_role_template_versions",
                table: "tenant_roles",
                column: "role_template_version_id",
                principalTable: "role_template_versions",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_tenant_user_permissions_permission_definition_id_permission_definitions",
                table: "tenant_user_permissions",
                column: "permission_definition_id",
                principalTable: "permission_definitions",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_tenant_user_permissions_tenant_user_id_tenant_users",
                table: "tenant_user_permissions",
                column: "tenant_user_id",
                principalTable: "tenant_users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_tenant_user_roles_tenant_role_id_tenant_roles",
                table: "tenant_user_roles",
                column: "tenant_role_id",
                principalTable: "tenant_roles",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_tenant_user_roles_tenant_user_id_tenant_users",
                table: "tenant_user_roles",
                column: "tenant_user_id",
                principalTable: "tenant_users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_tenants_business_type_id_business_types",
                table: "tenants",
                column: "business_type_id",
                principalTable: "business_types",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_user_invites_tenant_user_id_tenant_users",
                table: "user_invites",
                column: "tenant_user_id",
                principalTable: "tenant_users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_user_setup_tokens_user_invite_id_user_invites",
                table: "user_setup_tokens",
                column: "user_invite_id",
                principalTable: "user_invites",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
