using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E_POS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class OutletTillDeviceRefactoring : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "code_sequences");

            migrationBuilder.DropIndex(
                name: "uq_tills_tenant_id_id",
                table: "tills");

            migrationBuilder.DropIndex(
                name: "uq_till_device_assignments_active_pos_device_id",
                table: "till_device_assignments");

            migrationBuilder.DropIndex(
                name: "uq_till_device_assignments_active_till_id_pos_device_id",
                table: "till_device_assignments");

            migrationBuilder.DropIndex(
                name: "uq_till_device_assignments_till_id_pos_device_id_effective_from",
                table: "till_device_assignments");

            migrationBuilder.DropCheckConstraint(
                name: "ck_till_device_assignments_status",
                table: "till_device_assignments");

            migrationBuilder.DropIndex(
                name: "uq_pos_devices_device_serial_number",
                table: "pos_devices");

            migrationBuilder.DropIndex(
                name: "uq_pos_devices_tenant_id_id",
                table: "pos_devices");

            migrationBuilder.DropIndex(
                name: "uq_outlets_tenant_id_id",
                table: "outlets");

            migrationBuilder.DropIndex(
                name: "uq_outlet_business_hours_outlet_id_day_of_week",
                table: "outlet_business_hours");

            migrationBuilder.DropCheckConstraint(
                name: "ck_outlet_business_hours_open_time_close_time",
                table: "outlet_business_hours");

            migrationBuilder.DropIndex(
                name: "uq_hardware_profiles_tenant_id_profile_code",
                table: "hardware_profiles");

            migrationBuilder.DropColumn(
                name: "name",
                table: "tills");

            migrationBuilder.DropColumn(
                name: "effective_from",
                table: "till_device_assignments");

            migrationBuilder.DropColumn(
                name: "status",
                table: "till_device_assignments");

            migrationBuilder.DropColumn(
                name: "device_serial_number",
                table: "pos_devices");

            migrationBuilder.DropColumn(
                name: "name",
                table: "pos_devices");

            migrationBuilder.DropColumn(
                name: "close_time",
                table: "outlet_business_hours");

            migrationBuilder.DropColumn(
                name: "open_time",
                table: "outlet_business_hours");

            migrationBuilder.DropColumn(
                name: "name",
                table: "hardware_profiles");

            migrationBuilder.DropColumn(
                name: "profile_code",
                table: "hardware_profiles");

            migrationBuilder.RenameColumn(
                name: "effective_to",
                table: "till_device_assignments",
                newName: "released_at");

            migrationBuilder.RenameColumn(
                name: "name",
                table: "outlets",
                newName: "outlet_name");

            migrationBuilder.RenameColumn(
                name: "is_online_visible",
                table: "outlets",
                newName: "is_default_outlet");

            migrationBuilder.RenameColumn(
                name: "contact_phone",
                table: "outlets",
                newName: "phone");

            migrationBuilder.RenameColumn(
                name: "contact_email",
                table: "outlets",
                newName: "email");

            migrationBuilder.RenameColumn(
                name: "address_line_2",
                table: "outlet_addresses",
                newName: "address_line2");

            migrationBuilder.RenameColumn(
                name: "address_line_1",
                table: "outlet_addresses",
                newName: "address_line1");

            migrationBuilder.AlterColumn<string>(
                name: "till_code",
                table: "tills",
                type: "varchar(60)",
                maxLength: 60,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(80)",
                oldMaxLength: 80);

            migrationBuilder.AlterColumn<string>(
                name: "status",
                table: "tills",
                type: "varchar(40)",
                maxLength: 40,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(30)",
                oldMaxLength: 30);

            migrationBuilder.AlterColumn<Guid>(
                name: "outlet_id",
                table: "tills",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "currency_code",
                table: "tills",
                type: "char(3)",
                maxLength: 3,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "default_opening_float_amount",
                table: "tills",
                type: "numeric(18,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<bool>(
                name: "is_cash_managed",
                table: "tills",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<string>(
                name: "till_name",
                table: "tills",
                type: "varchar(150)",
                maxLength: 150,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "till_type",
                table: "tills",
                type: "varchar(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<Guid>(
                name: "till_id",
                table: "till_device_assignments",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "pos_device_id",
                table: "till_device_assignments",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "assigned_at",
                table: "till_device_assignments",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<Guid>(
                name: "assigned_by_tenant_user_id",
                table: "till_device_assignments",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "outlet_id",
                table: "till_device_assignments",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "release_reason",
                table: "till_device_assignments",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "released_by_tenant_user_id",
                table: "till_device_assignments",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "tenant_id",
                table: "till_device_assignments",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AlterColumn<string>(
                name: "status",
                table: "pos_devices",
                type: "varchar(40)",
                maxLength: 40,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(30)",
                oldMaxLength: 30);

            migrationBuilder.AddColumn<string>(
                name: "app_version",
                table: "pos_devices",
                type: "varchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "device_fingerprint_hash",
                table: "pos_devices",
                type: "varchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "device_name",
                table: "pos_devices",
                type: "varchar(150)",
                maxLength: 150,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "device_type",
                table: "pos_devices",
                type: "varchar(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "is_trusted",
                table: "pos_devices",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "last_seen_at",
                table: "pos_devices",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "paired_at",
                table: "pos_devices",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "paired_by_tenant_user_id",
                table: "pos_devices",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "platform",
                table: "pos_devices",
                type: "varchar(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "unpaired_at",
                table: "pos_devices",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "unpaired_by_tenant_user_id",
                table: "pos_devices",
                type: "uuid",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "status",
                table: "outlets",
                type: "varchar(40)",
                maxLength: 40,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(30)",
                oldMaxLength: 30);

            migrationBuilder.AlterColumn<string>(
                name: "outlet_type",
                table: "outlets",
                type: "varchar(40)",
                maxLength: 40,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(40)",
                oldMaxLength: 40,
                oldDefaultValue: "STORE");

            migrationBuilder.AlterColumn<string>(
                name: "outlet_code",
                table: "outlets",
                type: "varchar(60)",
                maxLength: 60,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(80)",
                oldMaxLength: 80);

            migrationBuilder.AddColumn<string>(
                name: "timezone",
                table: "outlets",
                type: "varchar(80)",
                maxLength: 80,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<Guid>(
                name: "outlet_id",
                table: "outlet_business_hours",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<short>(
                name: "day_of_week",
                table: "outlet_business_hours",
                type: "smallint",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<TimeOnly>(
                name: "closing_time",
                table: "outlet_business_hours",
                type: "time without time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_closed",
                table: "outlet_business_hours",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<TimeOnly>(
                name: "opening_time",
                table: "outlet_business_hours",
                type: "time without time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "tenant_id",
                table: "outlet_business_hours",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<DateOnly>(
                name: "valid_from",
                table: "outlet_business_hours",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "valid_until",
                table: "outlet_business_hours",
                type: "date",
                nullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "outlet_id",
                table: "outlet_addresses",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "country_code",
                table: "outlet_addresses",
                type: "char(2)",
                maxLength: 2,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "char(2)",
                oldMaxLength: 2,
                oldDefaultValue: "LK");

            migrationBuilder.AlterColumn<string>(
                name: "city",
                table: "outlet_addresses",
                type: "varchar(120)",
                maxLength: 120,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(120)",
                oldMaxLength: 120,
                oldDefaultValue: "");

            migrationBuilder.AlterColumn<string>(
                name: "address_line1",
                table: "outlet_addresses",
                type: "varchar(255)",
                maxLength: 255,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(255)",
                oldMaxLength: 255,
                oldDefaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "contact_name",
                table: "outlet_addresses",
                type: "varchar(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "contact_phone",
                table: "outlet_addresses",
                type: "varchar(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_primary",
                table: "outlet_addresses",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "status",
                table: "outlet_addresses",
                type: "varchar(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "tenant_id",
                table: "outlet_addresses",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "configuration_json",
                table: "hardware_profiles",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "description",
                table: "hardware_profiles",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "profile_name",
                table: "hardware_profiles",
                type: "varchar(150)",
                maxLength: 150,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "profile_type",
                table: "hardware_profiles",
                type: "varchar(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "status",
                table: "hardware_profiles",
                type: "varchar(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_till_device_assignments_outlet_id",
                table: "till_device_assignments",
                column: "outlet_id");

            migrationBuilder.CreateIndex(
                name: "IX_till_device_assignments_tenant_id",
                table: "till_device_assignments",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "uq_till_device_assignments_active_pos_device",
                table: "till_device_assignments",
                column: "pos_device_id",
                unique: true,
                filter: "released_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "uq_till_device_assignments_active_till",
                table: "till_device_assignments",
                column: "till_id",
                unique: true,
                filter: "released_at IS NULL");

            migrationBuilder.AddCheckConstraint(
                name: "ck_pos_devices_status",
                table: "pos_devices",
                sql: "status IN ('ACTIVE', 'INACTIVE', 'DELETED')");

            migrationBuilder.CreateIndex(
                name: "IX_outlet_business_hours_outlet_id",
                table: "outlet_business_hours",
                column: "outlet_id");

            migrationBuilder.CreateIndex(
                name: "IX_outlet_business_hours_tenant_id",
                table: "outlet_business_hours",
                column: "tenant_id");

            migrationBuilder.AddCheckConstraint(
                name: "ck_outlet_business_hours_validity",
                table: "outlet_business_hours",
                sql: "valid_until IS NULL OR valid_from IS NULL OR valid_until >= valid_from");

            migrationBuilder.CreateIndex(
                name: "IX_outlet_addresses_tenant_id",
                table: "outlet_addresses",
                column: "tenant_id");

            migrationBuilder.AddCheckConstraint(
                name: "ck_outlet_addresses_status",
                table: "outlet_addresses",
                sql: "status IN ('ACTIVE', 'INACTIVE', 'DELETED')");

            migrationBuilder.CreateIndex(
                name: "uq_hardware_profiles_tenant_id_profile_name",
                table: "hardware_profiles",
                columns: new[] { "tenant_id", "profile_name" },
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_hardware_profiles_status",
                table: "hardware_profiles",
                sql: "status IN ('ACTIVE', 'INACTIVE', 'DELETED')");

            migrationBuilder.AddForeignKey(
                name: "fk_outlet_addresses_tenant_id_tenants",
                table: "outlet_addresses",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_outlet_business_hours_tenant_id_tenants",
                table: "outlet_business_hours",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_pos_devices_tenant_id_tenants",
                table: "pos_devices",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_till_device_assignments_outlet_id_outlets",
                table: "till_device_assignments",
                column: "outlet_id",
                principalTable: "outlets",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_till_device_assignments_tenant_id_tenants",
                table: "till_device_assignments",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_tills_tenant_id_tenants",
                table: "tills",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_outlet_addresses_tenant_id_tenants",
                table: "outlet_addresses");

            migrationBuilder.DropForeignKey(
                name: "fk_outlet_business_hours_tenant_id_tenants",
                table: "outlet_business_hours");

            migrationBuilder.DropForeignKey(
                name: "fk_pos_devices_tenant_id_tenants",
                table: "pos_devices");

            migrationBuilder.DropForeignKey(
                name: "fk_till_device_assignments_outlet_id_outlets",
                table: "till_device_assignments");

            migrationBuilder.DropForeignKey(
                name: "fk_till_device_assignments_tenant_id_tenants",
                table: "till_device_assignments");

            migrationBuilder.DropForeignKey(
                name: "fk_tills_tenant_id_tenants",
                table: "tills");

            migrationBuilder.DropIndex(
                name: "IX_till_device_assignments_outlet_id",
                table: "till_device_assignments");

            migrationBuilder.DropIndex(
                name: "IX_till_device_assignments_tenant_id",
                table: "till_device_assignments");

            migrationBuilder.DropIndex(
                name: "uq_till_device_assignments_active_pos_device",
                table: "till_device_assignments");

            migrationBuilder.DropIndex(
                name: "uq_till_device_assignments_active_till",
                table: "till_device_assignments");

            migrationBuilder.DropCheckConstraint(
                name: "ck_pos_devices_status",
                table: "pos_devices");

            migrationBuilder.DropIndex(
                name: "IX_outlet_business_hours_outlet_id",
                table: "outlet_business_hours");

            migrationBuilder.DropIndex(
                name: "IX_outlet_business_hours_tenant_id",
                table: "outlet_business_hours");

            migrationBuilder.DropCheckConstraint(
                name: "ck_outlet_business_hours_validity",
                table: "outlet_business_hours");

            migrationBuilder.DropIndex(
                name: "IX_outlet_addresses_tenant_id",
                table: "outlet_addresses");

            migrationBuilder.DropCheckConstraint(
                name: "ck_outlet_addresses_status",
                table: "outlet_addresses");

            migrationBuilder.DropIndex(
                name: "uq_hardware_profiles_tenant_id_profile_name",
                table: "hardware_profiles");

            migrationBuilder.DropCheckConstraint(
                name: "ck_hardware_profiles_status",
                table: "hardware_profiles");

            migrationBuilder.DropColumn(
                name: "currency_code",
                table: "tills");

            migrationBuilder.DropColumn(
                name: "default_opening_float_amount",
                table: "tills");

            migrationBuilder.DropColumn(
                name: "is_cash_managed",
                table: "tills");

            migrationBuilder.DropColumn(
                name: "till_name",
                table: "tills");

            migrationBuilder.DropColumn(
                name: "till_type",
                table: "tills");

            migrationBuilder.DropColumn(
                name: "assigned_at",
                table: "till_device_assignments");

            migrationBuilder.DropColumn(
                name: "assigned_by_tenant_user_id",
                table: "till_device_assignments");

            migrationBuilder.DropColumn(
                name: "outlet_id",
                table: "till_device_assignments");

            migrationBuilder.DropColumn(
                name: "release_reason",
                table: "till_device_assignments");

            migrationBuilder.DropColumn(
                name: "released_by_tenant_user_id",
                table: "till_device_assignments");

            migrationBuilder.DropColumn(
                name: "tenant_id",
                table: "till_device_assignments");

            migrationBuilder.DropColumn(
                name: "app_version",
                table: "pos_devices");

            migrationBuilder.DropColumn(
                name: "device_fingerprint_hash",
                table: "pos_devices");

            migrationBuilder.DropColumn(
                name: "device_name",
                table: "pos_devices");

            migrationBuilder.DropColumn(
                name: "device_type",
                table: "pos_devices");

            migrationBuilder.DropColumn(
                name: "is_trusted",
                table: "pos_devices");

            migrationBuilder.DropColumn(
                name: "last_seen_at",
                table: "pos_devices");

            migrationBuilder.DropColumn(
                name: "paired_at",
                table: "pos_devices");

            migrationBuilder.DropColumn(
                name: "paired_by_tenant_user_id",
                table: "pos_devices");

            migrationBuilder.DropColumn(
                name: "platform",
                table: "pos_devices");

            migrationBuilder.DropColumn(
                name: "unpaired_at",
                table: "pos_devices");

            migrationBuilder.DropColumn(
                name: "unpaired_by_tenant_user_id",
                table: "pos_devices");

            migrationBuilder.DropColumn(
                name: "timezone",
                table: "outlets");

            migrationBuilder.DropColumn(
                name: "closing_time",
                table: "outlet_business_hours");

            migrationBuilder.DropColumn(
                name: "is_closed",
                table: "outlet_business_hours");

            migrationBuilder.DropColumn(
                name: "opening_time",
                table: "outlet_business_hours");

            migrationBuilder.DropColumn(
                name: "tenant_id",
                table: "outlet_business_hours");

            migrationBuilder.DropColumn(
                name: "valid_from",
                table: "outlet_business_hours");

            migrationBuilder.DropColumn(
                name: "valid_until",
                table: "outlet_business_hours");

            migrationBuilder.DropColumn(
                name: "contact_name",
                table: "outlet_addresses");

            migrationBuilder.DropColumn(
                name: "contact_phone",
                table: "outlet_addresses");

            migrationBuilder.DropColumn(
                name: "is_primary",
                table: "outlet_addresses");

            migrationBuilder.DropColumn(
                name: "status",
                table: "outlet_addresses");

            migrationBuilder.DropColumn(
                name: "tenant_id",
                table: "outlet_addresses");

            migrationBuilder.DropColumn(
                name: "configuration_json",
                table: "hardware_profiles");

            migrationBuilder.DropColumn(
                name: "description",
                table: "hardware_profiles");

            migrationBuilder.DropColumn(
                name: "profile_name",
                table: "hardware_profiles");

            migrationBuilder.DropColumn(
                name: "profile_type",
                table: "hardware_profiles");

            migrationBuilder.DropColumn(
                name: "status",
                table: "hardware_profiles");

            migrationBuilder.RenameColumn(
                name: "released_at",
                table: "till_device_assignments",
                newName: "effective_to");

            migrationBuilder.RenameColumn(
                name: "phone",
                table: "outlets",
                newName: "contact_phone");

            migrationBuilder.RenameColumn(
                name: "outlet_name",
                table: "outlets",
                newName: "name");

            migrationBuilder.RenameColumn(
                name: "is_default_outlet",
                table: "outlets",
                newName: "is_online_visible");

            migrationBuilder.RenameColumn(
                name: "email",
                table: "outlets",
                newName: "contact_email");

            migrationBuilder.RenameColumn(
                name: "address_line2",
                table: "outlet_addresses",
                newName: "address_line_2");

            migrationBuilder.RenameColumn(
                name: "address_line1",
                table: "outlet_addresses",
                newName: "address_line_1");

            migrationBuilder.AlterColumn<string>(
                name: "till_code",
                table: "tills",
                type: "varchar(80)",
                maxLength: 80,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(60)",
                oldMaxLength: 60);

            migrationBuilder.AlterColumn<string>(
                name: "status",
                table: "tills",
                type: "varchar(30)",
                maxLength: 30,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(40)",
                oldMaxLength: 40);

            migrationBuilder.AlterColumn<Guid>(
                name: "outlet_id",
                table: "tills",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<string>(
                name: "name",
                table: "tills",
                type: "varchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<Guid>(
                name: "till_id",
                table: "till_device_assignments",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<Guid>(
                name: "pos_device_id",
                table: "till_device_assignments",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<string>(
                name: "effective_from",
                table: "till_device_assignments",
                type: "varchar(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "status",
                table: "till_device_assignments",
                type: "varchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "ACTIVE");

            migrationBuilder.AlterColumn<string>(
                name: "status",
                table: "pos_devices",
                type: "varchar(30)",
                maxLength: 30,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(40)",
                oldMaxLength: 40);

            migrationBuilder.AddColumn<string>(
                name: "device_serial_number",
                table: "pos_devices",
                type: "varchar(80)",
                maxLength: 80,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "name",
                table: "pos_devices",
                type: "varchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<string>(
                name: "status",
                table: "outlets",
                type: "varchar(30)",
                maxLength: 30,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(40)",
                oldMaxLength: 40);

            migrationBuilder.AlterColumn<string>(
                name: "outlet_type",
                table: "outlets",
                type: "varchar(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "STORE",
                oldClrType: typeof(string),
                oldType: "varchar(40)",
                oldMaxLength: 40);

            migrationBuilder.AlterColumn<string>(
                name: "outlet_code",
                table: "outlets",
                type: "varchar(80)",
                maxLength: 80,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(60)",
                oldMaxLength: 60);

            migrationBuilder.AlterColumn<Guid>(
                name: "outlet_id",
                table: "outlet_business_hours",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<int>(
                name: "day_of_week",
                table: "outlet_business_hours",
                type: "integer",
                nullable: false,
                oldClrType: typeof(short),
                oldType: "smallint");

            migrationBuilder.AddColumn<TimeOnly>(
                name: "close_time",
                table: "outlet_business_hours",
                type: "time without time zone",
                nullable: false,
                defaultValue: new TimeOnly(0, 0, 0));

            migrationBuilder.AddColumn<TimeOnly>(
                name: "open_time",
                table: "outlet_business_hours",
                type: "time without time zone",
                nullable: false,
                defaultValue: new TimeOnly(0, 0, 0));

            migrationBuilder.AlterColumn<Guid>(
                name: "outlet_id",
                table: "outlet_addresses",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<string>(
                name: "country_code",
                table: "outlet_addresses",
                type: "char(2)",
                maxLength: 2,
                nullable: false,
                defaultValue: "LK",
                oldClrType: typeof(string),
                oldType: "char(2)",
                oldMaxLength: 2);

            migrationBuilder.AlterColumn<string>(
                name: "city",
                table: "outlet_addresses",
                type: "varchar(120)",
                maxLength: 120,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "varchar(120)",
                oldMaxLength: 120);

            migrationBuilder.AlterColumn<string>(
                name: "address_line_1",
                table: "outlet_addresses",
                type: "varchar(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "varchar(255)",
                oldMaxLength: 255);

            migrationBuilder.AddColumn<string>(
                name: "name",
                table: "hardware_profiles",
                type: "varchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "profile_code",
                table: "hardware_profiles",
                type: "varchar(80)",
                maxLength: 80,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "code_sequences",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    current_value = table.Column<int>(type: "integer", nullable: false),
                    padding_length = table.Column<int>(type: "integer", nullable: false),
                    prefix = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    sequence_key = table.Column<string>(type: "varchar(160)", maxLength: 160, nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_code_sequences", x => x.id);
                    table.CheckConstraint("ck_code_sequences_current_value", "current_value >= 0");
                    table.CheckConstraint("ck_code_sequences_padding_length", "padding_length > 0");
                    table.ForeignKey(
                        name: "fk_code_sequences_tenant_id_tenants",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "uq_tills_tenant_id_id",
                table: "tills",
                columns: new[] { "tenant_id", "id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_till_device_assignments_active_pos_device_id",
                table: "till_device_assignments",
                column: "pos_device_id",
                unique: true,
                filter: "status = 'ACTIVE' AND pos_device_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "uq_till_device_assignments_active_till_id_pos_device_id",
                table: "till_device_assignments",
                columns: new[] { "till_id", "pos_device_id" },
                unique: true,
                filter: "status = 'ACTIVE' AND till_id IS NOT NULL AND pos_device_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "uq_till_device_assignments_till_id_pos_device_id_effective_from",
                table: "till_device_assignments",
                columns: new[] { "till_id", "pos_device_id", "effective_from" },
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_till_device_assignments_status",
                table: "till_device_assignments",
                sql: "status IN ('ACTIVE', 'REVOKED')");

            migrationBuilder.CreateIndex(
                name: "uq_pos_devices_device_serial_number",
                table: "pos_devices",
                column: "device_serial_number",
                unique: true,
                filter: "device_serial_number IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "uq_pos_devices_tenant_id_id",
                table: "pos_devices",
                columns: new[] { "tenant_id", "id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_outlets_tenant_id_id",
                table: "outlets",
                columns: new[] { "tenant_id", "id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_outlet_business_hours_outlet_id_day_of_week",
                table: "outlet_business_hours",
                columns: new[] { "outlet_id", "day_of_week" },
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_outlet_business_hours_open_time_close_time",
                table: "outlet_business_hours",
                sql: "open_time < close_time");

            migrationBuilder.CreateIndex(
                name: "uq_hardware_profiles_tenant_id_profile_code",
                table: "hardware_profiles",
                columns: new[] { "tenant_id", "profile_code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_code_sequences_tenant_id_sequence_key",
                table: "code_sequences",
                columns: new[] { "tenant_id", "sequence_key" },
                unique: true);
        }
    }
}
