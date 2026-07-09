using System;
using E_POS.Application.Common.Security;
using E_POS.Infrastructure.Persistence.Seed;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E_POS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTillActivationCodes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "till_activation_codes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    outlet_id = table.Column<Guid>(type: "uuid", nullable: false),
                    till_id = table.Column<Guid>(type: "uuid", nullable: false),
                    activation_code_hash = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false),
                    issued_by_tenant_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    used_by_pos_device_id = table.Column<Guid>(type: "uuid", nullable: true),
                    used_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_till_activation_codes", x => x.id);
                    table.CheckConstraint("ck_till_activation_codes_status", "status IN ('ACTIVE', 'USED', 'EXPIRED', 'REVOKED')");
                    table.ForeignKey(
                        name: "fk_till_activation_codes_issued_by_tenant_user_id_tenant_users",
                        column: x => x.issued_by_tenant_user_id,
                        principalTable: "tenant_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_till_activation_codes_outlet_id_outlets",
                        column: x => x.outlet_id,
                        principalTable: "outlets",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_till_activation_codes_tenant_id_tenants",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_till_activation_codes_till_id_tills",
                        column: x => x.till_id,
                        principalTable: "tills",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_till_activation_codes_used_by_pos_device_id_pos_devices",
                        column: x => x.used_by_pos_device_id,
                        principalTable: "pos_devices",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_till_activation_codes_issued_by_tenant_user_id",
                table: "till_activation_codes",
                column: "issued_by_tenant_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_till_activation_codes_outlet_id",
                table: "till_activation_codes",
                column: "outlet_id");

            migrationBuilder.CreateIndex(
                name: "ix_till_activation_codes_tenant_id_till_id_status",
                table: "till_activation_codes",
                columns: new[] { "tenant_id", "till_id", "status" });

            migrationBuilder.CreateIndex(
                name: "IX_till_activation_codes_till_id",
                table: "till_activation_codes",
                column: "till_id");

            migrationBuilder.CreateIndex(
                name: "IX_till_activation_codes_used_by_pos_device_id",
                table: "till_activation_codes",
                column: "used_by_pos_device_id");

            migrationBuilder.CreateIndex(
                name: "uq_till_activation_codes_activation_code_hash",
                table: "till_activation_codes",
                column: "activation_code_hash",
                unique: true);

            var activationCodeHash = DeviceFingerprintHasher.Hash(
                DevelopmentPosHomeContextSeedConstants.DevelopmentActivationCode);

            migrationBuilder.Sql($"""
                UPDATE pos_devices
                SET is_trusted = false,
                    device_fingerprint_hash = NULL,
                    paired_at = NULL,
                    paired_by_tenant_user_id = NULL,
                    updated_at = now()
                WHERE id = '{DevelopmentPosHomeContextSeedConstants.DevelopmentPosDeviceId}';

                INSERT INTO till_activation_codes (
                    id, tenant_id, outlet_id, till_id, activation_code_hash,
                    issued_by_tenant_user_id, status, expires_at, used_by_pos_device_id,
                    used_at, created_at
                )
                VALUES (
                    '{DevelopmentPosHomeContextSeedConstants.DevelopmentTillActivationCodeId}',
                    '{DevelopmentTenantSeedConstants.DevelopmentTenantId}',
                    '{DevelopmentPosHomeContextSeedConstants.DevelopmentOutletId}',
                    '{DevelopmentPosHomeContextSeedConstants.DevelopmentTillId}',
                    '{activationCodeHash}',
                    '99999999-0001-4000-8000-000000000001',
                    'ACTIVE',
                    now() + interval '365 days',
                    NULL,
                    NULL,
                    now()
                )
                ON CONFLICT (id) DO UPDATE
                SET activation_code_hash = EXCLUDED.activation_code_hash,
                    status = 'ACTIVE',
                    expires_at = EXCLUDED.expires_at,
                    used_by_pos_device_id = NULL,
                    used_at = NULL;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql($"""
                DELETE FROM till_activation_codes
                WHERE id = '{DevelopmentPosHomeContextSeedConstants.DevelopmentTillActivationCodeId}';

                UPDATE pos_devices
                SET is_trusted = true,
                    updated_at = now()
                WHERE id = '{DevelopmentPosHomeContextSeedConstants.DevelopmentPosDeviceId}';
                """);

            migrationBuilder.DropTable(
                name: "till_activation_codes");
        }
    }
}
