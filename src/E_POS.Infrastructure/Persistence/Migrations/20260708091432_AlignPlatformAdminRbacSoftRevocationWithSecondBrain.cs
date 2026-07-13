using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E_POS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AlignPlatformAdminRbacSoftRevocationWithSecondBrain : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DO $$
                BEGIN
                    IF EXISTS (
                        SELECT 1
                        FROM platform_user_roles
                        WHERE revoked_at IS NULL
                        GROUP BY platform_user_id, platform_role_id
                        HAVING COUNT(*) > 1
                    ) THEN
                        RAISE EXCEPTION 'Duplicate active platform_user_roles rows detected. Resolve before applying partial unique index.';
                    END IF;

                    IF EXISTS (
                        SELECT 1
                        FROM platform_role_permissions
                        WHERE revoked_at IS NULL
                        GROUP BY platform_role_id, platform_permission_id
                        HAVING COUNT(*) > 1
                    ) THEN
                        RAISE EXCEPTION 'Duplicate active platform_role_permissions rows detected. Resolve before applying partial unique index.';
                    END IF;

                    IF EXISTS (
                        SELECT 1
                        FROM platform_user_permissions
                        WHERE revoked_at IS NULL
                        GROUP BY platform_user_id, platform_permission_id
                        HAVING COUNT(*) > 1
                    ) THEN
                        RAISE EXCEPTION 'Duplicate active platform_user_permissions rows detected. Resolve before applying partial unique index.';
                    END IF;
                END $$;
                """);

            migrationBuilder.DropIndex(
                name: "uq_platform_user_roles_platform_user_id_platform_role_id",
                table: "platform_user_roles");

            migrationBuilder.DropIndex(
                name: "uq_platform_user_permissions_platform_user_id_platform_permission_id",
                table: "platform_user_permissions");

            migrationBuilder.DropIndex(
                name: "uq_platform_role_permissions_platform_role_id_platform_permission_id",
                table: "platform_role_permissions");

            migrationBuilder.CreateIndex(
                name: "uq_platform_user_roles_platform_user_id_platform_role_id",
                table: "platform_user_roles",
                columns: new[] { "platform_user_id", "platform_role_id" },
                unique: true,
                filter: "revoked_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "uq_platform_user_permissions_platform_user_id_platform_permission_id",
                table: "platform_user_permissions",
                columns: new[] { "platform_user_id", "platform_permission_id" },
                unique: true,
                filter: "revoked_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "uq_platform_role_permissions_platform_role_id_platform_permission_id",
                table: "platform_role_permissions",
                columns: new[] { "platform_role_id", "platform_permission_id" },
                unique: true,
                filter: "revoked_at IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "uq_platform_user_roles_platform_user_id_platform_role_id",
                table: "platform_user_roles");

            migrationBuilder.DropIndex(
                name: "uq_platform_user_permissions_platform_user_id_platform_permission_id",
                table: "platform_user_permissions");

            migrationBuilder.DropIndex(
                name: "uq_platform_role_permissions_platform_role_id_platform_permission_id",
                table: "platform_role_permissions");

            migrationBuilder.CreateIndex(
                name: "uq_platform_user_roles_platform_user_id_platform_role_id",
                table: "platform_user_roles",
                columns: new[] { "platform_user_id", "platform_role_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_platform_user_permissions_platform_user_id_platform_permission_id",
                table: "platform_user_permissions",
                columns: new[] { "platform_user_id", "platform_permission_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_platform_role_permissions_platform_role_id_platform_permission_id",
                table: "platform_role_permissions",
                columns: new[] { "platform_role_id", "platform_permission_id" },
                unique: true);
        }
    }
}
