using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E_POS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SeedNewActivationCode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            var activationCodeHash = E_POS.Application.Common.Security.DeviceFingerprintHasher.Hash(
                E_POS.Infrastructure.Persistence.Seed.DevelopmentPosHomeContextSeedConstants.DevelopmentActivationCode2);

            migrationBuilder.Sql($"""
                INSERT INTO till_activation_codes (
                    id, tenant_id, outlet_id, till_id, activation_code_hash,
                    issued_by_tenant_user_id, status, expires_at, used_by_pos_device_id,
                    used_at, created_at
                )
                VALUES (
                    '{E_POS.Infrastructure.Persistence.Seed.DevelopmentPosHomeContextSeedConstants.DevelopmentTillActivationCodeId2}',
                    '{E_POS.Infrastructure.Persistence.Seed.DevelopmentTenantSeedConstants.DevelopmentTenantId}',
                    '{E_POS.Infrastructure.Persistence.Seed.DevelopmentPosHomeContextSeedConstants.DevelopmentOutletId}',
                    '{E_POS.Infrastructure.Persistence.Seed.DevelopmentPosHomeContextSeedConstants.DevelopmentTillId}',
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
                WHERE id = '{E_POS.Infrastructure.Persistence.Seed.DevelopmentPosHomeContextSeedConstants.DevelopmentTillActivationCodeId2}';
                """);
        }
    }
}
