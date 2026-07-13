using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E_POS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class HardenPlatformAdminRefreshTokenChainPartialUniqueIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DO $$
                BEGIN
                    IF EXISTS (
                        SELECT 1
                        FROM platform_refresh_tokens
                        WHERE status = 'ACTIVE'
                        GROUP BY platform_auth_session_id
                        HAVING COUNT(*) > 1
                    ) THEN
                        RAISE EXCEPTION 'Duplicate ACTIVE refresh tokens per platform_auth_session_id detected. Resolve before applying 8G-c partial unique indexes.';
                    END IF;

                    IF EXISTS (
                        SELECT 1
                        FROM platform_refresh_tokens
                        WHERE status = 'ACTIVE'
                        GROUP BY token_family_id
                        HAVING COUNT(*) > 1
                    ) THEN
                        RAISE EXCEPTION 'Duplicate ACTIVE refresh tokens per token_family_id detected. Resolve before applying 8G-c partial unique indexes.';
                    END IF;

                    IF EXISTS (
                        SELECT 1
                        FROM platform_refresh_tokens
                        WHERE replaced_by_token_id IS NOT NULL
                        GROUP BY replaced_by_token_id
                        HAVING COUNT(*) > 1
                    ) THEN
                        RAISE EXCEPTION 'Duplicate replaced_by_token_id values detected. Resolve before applying 8G-c partial unique indexes.';
                    END IF;

                    IF EXISTS (
                        SELECT 1
                        FROM platform_refresh_tokens t
                        WHERE t.replaced_by_token_id IS NOT NULL
                          AND NOT EXISTS (
                              SELECT 1
                              FROM platform_refresh_tokens r
                              WHERE r.id = t.replaced_by_token_id
                          )
                    ) THEN
                        RAISE EXCEPTION 'Orphan replaced_by_token_id references detected. Resolve before applying 8G-c partial unique indexes.';
                    END IF;
                END $$;
                """);

            migrationBuilder.DropIndex(
                name: "IX_platform_refresh_tokens_replaced_by_token_id",
                table: "platform_refresh_tokens");

            migrationBuilder.CreateIndex(
                name: "uq_platform_refresh_tokens_platform_auth_session_id_active",
                table: "platform_refresh_tokens",
                column: "platform_auth_session_id",
                unique: true,
                filter: "status = 'ACTIVE'");

            migrationBuilder.CreateIndex(
                name: "uq_platform_refresh_tokens_replaced_by_token_id",
                table: "platform_refresh_tokens",
                column: "replaced_by_token_id",
                unique: true,
                filter: "replaced_by_token_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "uq_platform_refresh_tokens_token_family_id_active",
                table: "platform_refresh_tokens",
                column: "token_family_id",
                unique: true,
                filter: "status = 'ACTIVE'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "uq_platform_refresh_tokens_platform_auth_session_id_active",
                table: "platform_refresh_tokens");

            migrationBuilder.DropIndex(
                name: "uq_platform_refresh_tokens_replaced_by_token_id",
                table: "platform_refresh_tokens");

            migrationBuilder.DropIndex(
                name: "uq_platform_refresh_tokens_token_family_id_active",
                table: "platform_refresh_tokens");

            migrationBuilder.CreateIndex(
                name: "IX_platform_refresh_tokens_replaced_by_token_id",
                table: "platform_refresh_tokens",
                column: "replaced_by_token_id");
        }
    }
}
