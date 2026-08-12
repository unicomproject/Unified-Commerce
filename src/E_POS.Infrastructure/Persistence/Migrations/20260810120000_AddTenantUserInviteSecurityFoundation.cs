using System;
using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E_POS.Infrastructure.Persistence.Migrations;

[DbContext(typeof(EPosDbContext))]
[Migration("20260810120000_AddTenantUserInviteSecurityFoundation")]
public partial class AddTenantUserInviteSecurityFoundation : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(name: "employee_id", table: "tenant_users", type: "varchar(100)", maxLength: 100, nullable: true);
        migrationBuilder.AddColumn<string>(name: "staff_code", table: "tenant_users", type: "varchar(20)", maxLength: 20, nullable: true);
        migrationBuilder.AlterColumn<string>(name: "default_outlet_id", table: "tenant_users", type: "varchar(50)", maxLength: 50, nullable: true, oldClrType: typeof(string), oldType: "varchar(50)", oldMaxLength: 50);
        migrationBuilder.Sql("""
            WITH numbered AS (
              SELECT id, 'USR-' || to_char(created_at AT TIME ZONE 'UTC', 'YYYY') || '-' ||
                lpad(row_number() OVER (PARTITION BY tenant_id, to_char(created_at AT TIME ZONE 'UTC', 'YYYY') ORDER BY created_at, id)::text, 5, '0') AS code
              FROM tenant_users WHERE staff_code IS NULL
            ) UPDATE tenant_users u SET staff_code = numbered.code FROM numbered WHERE u.id = numbered.id;
            """);
        migrationBuilder.CreateIndex(name: "uq_tenant_users_tenant_id_staff_code", table: "tenant_users", columns: new[] { "tenant_id", "staff_code" }, unique: true, filter: "staff_code IS NOT NULL");

        migrationBuilder.CreateTable(name: "tenant_user_code_sequences", columns: table => new
        {
            id = table.Column<Guid>(nullable: false), tenant_id = table.Column<Guid>(nullable: false), sequence_type = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false), year = table.Column<int>(nullable: false), current_value = table.Column<long>(nullable: false), created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false), updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
        }, constraints: table => table.PrimaryKey("pk_tenant_user_code_sequences", x => x.id));
        migrationBuilder.CreateIndex(name: "uq_tenant_user_code_sequences_scope", table: "tenant_user_code_sequences", columns: new[] { "tenant_id", "sequence_type", "year" }, unique: true);

        migrationBuilder.AddColumn<Guid>(name: "tenant_user_id", table: "user_invites", nullable: true);
        migrationBuilder.CreateIndex(name: "ix_user_invites_tenant_user_id", table: "user_invites", column: "tenant_user_id");
        migrationBuilder.AddForeignKey(name: "fk_user_invites_tenant_user_id_tenant_users", table: "user_invites", column: "tenant_user_id", principalTable: "tenant_users", principalColumn: "id", onDelete: ReferentialAction.Restrict);

        migrationBuilder.CreateTable(name: "tenant_user_invite_delivery_secrets", columns: table => new
        {
            id = table.Column<Guid>(nullable: false), tenant_id = table.Column<Guid>(nullable: false), tenant_user_id = table.Column<Guid>(nullable: false), invite_id = table.Column<Guid>(nullable: false), encrypted_token = table.Column<string>(type: "text", nullable: false), key_version = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false), expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false), purged_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true), created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false), updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
        }, constraints: table =>
        {
            table.PrimaryKey("pk_tenant_user_invite_delivery_secrets", x => x.id);
            table.ForeignKey("fk_tenant_user_invite_delivery_secrets_user", x => x.tenant_user_id, "tenant_users", "id", onDelete: ReferentialAction.Restrict);
            table.ForeignKey("fk_tenant_user_invite_delivery_secrets_invite", x => x.invite_id, "user_invites", "id", onDelete: ReferentialAction.Restrict);
        });
        migrationBuilder.CreateIndex(name: "uq_tenant_user_invite_delivery_secrets_invite_id", table: "tenant_user_invite_delivery_secrets", column: "invite_id", unique: true);
        migrationBuilder.CreateIndex(name: "ix_tenant_user_invite_delivery_secrets_target", table: "tenant_user_invite_delivery_secrets", columns: new[] { "tenant_id", "tenant_user_id" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "tenant_user_invite_delivery_secrets");
        migrationBuilder.DropTable(name: "tenant_user_code_sequences");
        migrationBuilder.DropForeignKey(name: "fk_user_invites_tenant_user_id_tenant_users", table: "user_invites");
        migrationBuilder.DropIndex(name: "ix_user_invites_tenant_user_id", table: "user_invites");
        migrationBuilder.DropColumn(name: "tenant_user_id", table: "user_invites");
        migrationBuilder.DropIndex(name: "uq_tenant_users_tenant_id_staff_code", table: "tenant_users");
        migrationBuilder.DropColumn(name: "employee_id", table: "tenant_users");
        migrationBuilder.DropColumn(name: "staff_code", table: "tenant_users");
        migrationBuilder.AlterColumn<string>(name: "default_outlet_id", table: "tenant_users", type: "varchar(50)", maxLength: 50, nullable: false, defaultValue: "", oldClrType: typeof(string), oldType: "varchar(50)", oldMaxLength: 50, oldNullable: true);
    }
}
