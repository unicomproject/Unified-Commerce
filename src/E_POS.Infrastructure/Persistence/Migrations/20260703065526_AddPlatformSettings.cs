using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E_POS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPlatformSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "platform_settings",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    setting_key = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: false),
                    setting_value = table.Column<string>(type: "jsonb", nullable: false),
                    is_secret = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    updated_by_platform_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_platform_settings", x => x.id);
                    table.ForeignKey(
                        name: "fk_platform_settings_updated_by_platform_user_id_platform_users",
                        column: x => x.updated_by_platform_user_id,
                        principalTable: "platform_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_platform_settings_updated_by_platform_user_id",
                table: "platform_settings",
                column: "updated_by_platform_user_id");

            migrationBuilder.CreateIndex(
                name: "uq_platform_settings_setting_key",
                table: "platform_settings",
                column: "setting_key",
                unique: true);

            migrationBuilder.Sql("""
                INSERT INTO platform_settings (id, setting_key, setting_value, is_secret, description, updated_by_platform_user_id, created_at, updated_at)
                VALUES
                    ('11111111-1111-1111-1111-111111111111', 'general.platform_display_name', '"SCS-TIX"'::jsonb, false, 'Platform display name shown in admin surfaces.', NULL, now(), now()),
                    ('22222222-2222-2222-2222-222222222222', 'general.default_country_code', '"LK"'::jsonb, false, 'Default ISO country code for new tenant defaults.', NULL, now(), now()),
                    ('33333333-3333-3333-3333-333333333333', 'general.default_currency_code', '"LKR"'::jsonb, false, 'Default ISO currency code for new tenant defaults.', NULL, now(), now()),
                    ('44444444-4444-4444-4444-444444444444', 'general.default_timezone', '"Asia/Colombo"'::jsonb, false, 'Default IANA timezone for new tenant defaults.', NULL, now(), now()),
                    ('55555555-5555-5555-5555-555555555555', 'general.default_locale', '"en-LK"'::jsonb, false, 'Default locale tag for new tenant defaults.', NULL, now(), now())
                ON CONFLICT (setting_key) DO NOTHING;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "platform_settings");
        }
    }
}
