using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E_POS.Infrastructure.Persistence.Migrations;

[DbContext(typeof(EPosDbContext))]
[Migration("20260826120000_AddTenantUserExplicitOutletTillAccess")]
public sealed class AddTenantUserExplicitOutletTillAccess : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<Guid>(
            name: "default_till_id",
            table: "tenant_users",
            type: "uuid",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "outlet_access_scope",
            table: "tenant_users",
            type: "varchar(30)",
            maxLength: 30,
            nullable: false,
            defaultValue: "ALL_OUTLETS");

        migrationBuilder.AddColumn<string>(
            name: "till_access_scope",
            table: "tenant_users",
            type: "varchar(30)",
            maxLength: 30,
            nullable: false,
            defaultValue: "ALL_ACCESSIBLE_TILLS");

        migrationBuilder.Sql(
            """
            UPDATE tenant_users AS users
            SET outlet_access_scope = 'SELECTED_OUTLETS'
            WHERE NOT EXISTS (
                    SELECT 1
                    FROM tenant_user_roles AS tenant_roles
                    WHERE tenant_roles.tenant_id = users.tenant_id
                      AND tenant_roles.user_id = users.id
                      AND tenant_roles.revoked_at IS NULL)
              AND EXISTS (
                    SELECT 1
                    FROM outlet_user_roles AS outlet_roles
                    WHERE outlet_roles.tenant_id = users.tenant_id
                      AND outlet_roles.user_id = users.id
                      AND outlet_roles.revoked_at IS NULL
                    UNION ALL
                    SELECT 1
                    FROM outlet_user_permissions AS outlet_permissions
                    WHERE outlet_permissions.tenant_id = users.tenant_id
                      AND outlet_permissions.user_id = users.id
                      AND outlet_permissions.revoked_at IS NULL);
            """);

        migrationBuilder.CreateTable(
            name: "tenant_user_till_access",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                tenant_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                till_id = table.Column<Guid>(type: "uuid", nullable: false),
                assigned_by_tenant_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                revoked_by_tenant_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                assigned_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                revoked_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_tenant_user_till_access", x => x.id);
                table.ForeignKey(
                    name: "fk_tenant_user_till_access_assigned_by_tenant_user_id",
                    column: x => x.assigned_by_tenant_user_id,
                    principalTable: "tenant_users",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "fk_tenant_user_till_access_revoked_by_tenant_user_id",
                    column: x => x.revoked_by_tenant_user_id,
                    principalTable: "tenant_users",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "fk_tenant_user_till_access_tenant_id_tenants",
                    column: x => x.tenant_id,
                    principalTable: "tenants",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "fk_tenant_user_till_access_tenant_user_id_tenant_users",
                    column: x => x.tenant_user_id,
                    principalTable: "tenant_users",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "fk_tenant_user_till_access_till_id_tills",
                    column: x => x.till_id,
                    principalTable: "tills",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "IX_tenant_users_default_till_id",
            table: "tenant_users",
            column: "default_till_id");

        migrationBuilder.CreateIndex(
            name: "IX_tenant_user_till_access_assigned_by_tenant_user_id",
            table: "tenant_user_till_access",
            column: "assigned_by_tenant_user_id");
        migrationBuilder.CreateIndex(
            name: "IX_tenant_user_till_access_revoked_by_tenant_user_id",
            table: "tenant_user_till_access",
            column: "revoked_by_tenant_user_id");
        migrationBuilder.CreateIndex(
            name: "ix_tenant_user_till_access_tenant_till",
            table: "tenant_user_till_access",
            columns: new[] { "tenant_id", "till_id" });
        migrationBuilder.CreateIndex(
            name: "IX_tenant_user_till_access_tenant_user_id",
            table: "tenant_user_till_access",
            column: "tenant_user_id");
        migrationBuilder.CreateIndex(
            name: "IX_tenant_user_till_access_till_id",
            table: "tenant_user_till_access",
            column: "till_id");
        migrationBuilder.CreateIndex(
            name: "uq_tenant_user_till_access_tenant_user_till",
            table: "tenant_user_till_access",
            columns: new[] { "tenant_id", "tenant_user_id", "till_id" },
            unique: true);

        migrationBuilder.AddForeignKey(
            name: "fk_tenant_users_default_till_id_tills",
            table: "tenant_users",
            column: "default_till_id",
            principalTable: "tills",
            principalColumn: "id",
            onDelete: ReferentialAction.Restrict);

        migrationBuilder.AddCheckConstraint(
            name: "ck_tenant_users_outlet_access_scope",
            table: "tenant_users",
            sql: "outlet_access_scope IN ('ALL_OUTLETS', 'SELECTED_OUTLETS', 'NO_OUTLET_ACCESS')");
        migrationBuilder.AddCheckConstraint(
            name: "ck_tenant_users_till_access_scope",
            table: "tenant_users",
            sql: "till_access_scope IN ('ALL_ACCESSIBLE_TILLS', 'SELECTED_TILLS', 'NO_TILL_ACCESS')");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "tenant_user_till_access");
        migrationBuilder.DropForeignKey(name: "fk_tenant_users_default_till_id_tills", table: "tenant_users");
        migrationBuilder.DropCheckConstraint(name: "ck_tenant_users_outlet_access_scope", table: "tenant_users");
        migrationBuilder.DropCheckConstraint(name: "ck_tenant_users_till_access_scope", table: "tenant_users");
        migrationBuilder.DropIndex(name: "IX_tenant_users_default_till_id", table: "tenant_users");
        migrationBuilder.DropColumn(name: "default_till_id", table: "tenant_users");
        migrationBuilder.DropColumn(name: "outlet_access_scope", table: "tenant_users");
        migrationBuilder.DropColumn(name: "till_access_scope", table: "tenant_users");
    }
}
