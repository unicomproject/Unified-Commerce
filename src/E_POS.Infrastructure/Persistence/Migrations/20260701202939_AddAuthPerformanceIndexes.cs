using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E_POS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAuthPerformanceIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_tenant_refresh_tokens_tenant_auth_session_id",
                table: "tenant_refresh_tokens");

            migrationBuilder.DropIndex(
                name: "IX_tenant_login_audits_tenant_user_id",
                table: "tenant_login_audits");

            migrationBuilder.DropIndex(
                name: "IX_platform_refresh_tokens_platform_auth_session_id",
                table: "platform_refresh_tokens");

            migrationBuilder.DropIndex(
                name: "IX_platform_login_audits_platform_user_id",
                table: "platform_login_audits");

            migrationBuilder.CreateIndex(
                name: "ix_tenant_refresh_tokens_tenant_auth_session_id_status",
                table: "tenant_refresh_tokens",
                columns: new[] { "tenant_auth_session_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_tenant_login_audits_tenant_user_id_login_result_created_at",
                table: "tenant_login_audits",
                columns: new[] { "tenant_user_id", "login_result", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_platform_refresh_tokens_platform_auth_session_id_status",
                table: "platform_refresh_tokens",
                columns: new[] { "platform_auth_session_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_platform_login_audits_platform_user_id_login_result_created_at",
                table: "platform_login_audits",
                columns: new[] { "platform_user_id", "login_result", "created_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_tenant_refresh_tokens_tenant_auth_session_id_status",
                table: "tenant_refresh_tokens");

            migrationBuilder.DropIndex(
                name: "ix_tenant_login_audits_tenant_user_id_login_result_created_at",
                table: "tenant_login_audits");

            migrationBuilder.DropIndex(
                name: "ix_platform_refresh_tokens_platform_auth_session_id_status",
                table: "platform_refresh_tokens");

            migrationBuilder.DropIndex(
                name: "ix_platform_login_audits_platform_user_id_login_result_created_at",
                table: "platform_login_audits");

            migrationBuilder.CreateIndex(
                name: "IX_tenant_refresh_tokens_tenant_auth_session_id",
                table: "tenant_refresh_tokens",
                column: "tenant_auth_session_id");

            migrationBuilder.CreateIndex(
                name: "IX_tenant_login_audits_tenant_user_id",
                table: "tenant_login_audits",
                column: "tenant_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_platform_refresh_tokens_platform_auth_session_id",
                table: "platform_refresh_tokens",
                column: "platform_auth_session_id");

            migrationBuilder.CreateIndex(
                name: "IX_platform_login_audits_platform_user_id",
                table: "platform_login_audits",
                column: "platform_user_id");
        }
    }
}
