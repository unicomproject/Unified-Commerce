using E_POS.Application.Common.Security;
using E_POS.Infrastructure.Persistence.Seed;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E_POS.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class SeedThirdDevelopmentTill : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        var activationCodeHash = DeviceFingerprintHasher.Hash(
            DevelopmentPosHomeContextSeedConstants.DevelopmentActivationCodeThree);

        migrationBuilder.Sql($"""
            INSERT INTO tills (
                id, tenant_id, outlet_id, till_area_name, till_number, till_name, till_code,
                till_type, default_opening_float_amount, currency_code, is_cash_managed, status,
                created_by_tenant_user_id, updated_by_tenant_user_id, created_at, updated_at
            )
            VALUES (
                '{DevelopmentPosHomeContextSeedConstants.DevelopmentTillThreeId}',
                '{DevelopmentTenantSeedConstants.DevelopmentTenantId}',
                '{DevelopmentPosHomeContextSeedConstants.DevelopmentOutletId}',
                'Back',
                3,
                'Back Till 03',
                'BACK-03',
                'STANDARD',
                0,
                'LKR',
                true,
                'ACTIVE',
                '{DevelopmentTenantSeedConstants.CashierUserId}',
                '{DevelopmentTenantSeedConstants.CashierUserId}',
                now(),
                now()
            )
            ON CONFLICT (tenant_id, outlet_id, till_code) DO UPDATE
            SET till_area_name = EXCLUDED.till_area_name,
                till_number = EXCLUDED.till_number,
                till_name = EXCLUDED.till_name,
                till_type = EXCLUDED.till_type,
                currency_code = EXCLUDED.currency_code,
                is_cash_managed = EXCLUDED.is_cash_managed,
                status = 'ACTIVE',
                updated_at = now();

            INSERT INTO pos_devices (
                id, tenant_id, outlet_id, device_name, device_code, device_type, platform,
                app_version, status, is_trusted, device_fingerprint_hash, paired_at,
                paired_by_tenant_user_id, created_by_tenant_user_id, updated_by_tenant_user_id,
                created_at, updated_at
            )
            VALUES (
                '{DevelopmentPosHomeContextSeedConstants.DevelopmentPosDeviceThreeId}',
                '{DevelopmentTenantSeedConstants.DevelopmentTenantId}',
                '{DevelopmentPosHomeContextSeedConstants.DevelopmentOutletId}',
                'Back POS Device 03',
                'POS-03',
                'TABLET',
                NULL,
                NULL,
                'ACTIVE',
                false,
                NULL,
                NULL,
                NULL,
                '{DevelopmentTenantSeedConstants.CashierUserId}',
                '{DevelopmentTenantSeedConstants.CashierUserId}',
                now(),
                now()
            )
            ON CONFLICT (tenant_id, device_code) DO UPDATE
            SET outlet_id = EXCLUDED.outlet_id,
                device_name = EXCLUDED.device_name,
                device_type = EXCLUDED.device_type,
                status = 'ACTIVE',
                is_trusted = false,
                device_fingerprint_hash = NULL,
                paired_at = NULL,
                paired_by_tenant_user_id = NULL,
                updated_at = now();

            INSERT INTO till_device_assignments (
                id, tenant_id, outlet_id, till_id, pos_device_id, assigned_at, released_at,
                created_at, updated_at
            )
            VALUES (
                '{DevelopmentPosHomeContextSeedConstants.DevelopmentTillDeviceAssignmentThreeId}',
                '{DevelopmentTenantSeedConstants.DevelopmentTenantId}',
                '{DevelopmentPosHomeContextSeedConstants.DevelopmentOutletId}',
                '{DevelopmentPosHomeContextSeedConstants.DevelopmentTillThreeId}',
                '{DevelopmentPosHomeContextSeedConstants.DevelopmentPosDeviceThreeId}',
                now(),
                NULL,
                now(),
                now()
            )
            ON CONFLICT (id) DO UPDATE
            SET outlet_id = EXCLUDED.outlet_id,
                till_id = EXCLUDED.till_id,
                pos_device_id = EXCLUDED.pos_device_id,
                assigned_at = EXCLUDED.assigned_at,
                released_at = NULL,
                updated_at = now();

            INSERT INTO till_sessions (
                id, tenant_id, outlet_id, till_id, session_number, business_date,
                opened_by_tenant_user_id, opened_from_pos_device_id, opening_float_amount,
                currency_code, status, opened_at, closed_at, created_at, updated_at
            )
            VALUES (
                '{DevelopmentPosHomeContextSeedConstants.DevelopmentTillSessionThreeId}',
                '{DevelopmentTenantSeedConstants.DevelopmentTenantId}',
                '{DevelopmentPosHomeContextSeedConstants.DevelopmentOutletId}',
                '{DevelopmentPosHomeContextSeedConstants.DevelopmentTillThreeId}',
                'TS-DEV-0003',
                CURRENT_DATE,
                '{DevelopmentTenantSeedConstants.CashierUserId}',
                '{DevelopmentPosHomeContextSeedConstants.DevelopmentPosDeviceThreeId}',
                0,
                'LKR',
                'OPEN',
                now(),
                NULL,
                now(),
                now()
            )
            ON CONFLICT (id) DO UPDATE
            SET business_date = CURRENT_DATE,
                opened_by_tenant_user_id = EXCLUDED.opened_by_tenant_user_id,
                opened_from_pos_device_id = EXCLUDED.opened_from_pos_device_id,
                status = 'OPEN',
                closed_at = NULL,
                updated_at = now();

            INSERT INTO till_activation_codes (
                id, tenant_id, outlet_id, till_id, activation_code_hash,
                issued_by_tenant_user_id, status, expires_at, used_by_pos_device_id,
                used_at, created_at
            )
            VALUES (
                '{DevelopmentPosHomeContextSeedConstants.DevelopmentTillActivationCodeThreeId}',
                '{DevelopmentTenantSeedConstants.DevelopmentTenantId}',
                '{DevelopmentPosHomeContextSeedConstants.DevelopmentOutletId}',
                '{DevelopmentPosHomeContextSeedConstants.DevelopmentTillThreeId}',
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
            WHERE id = '{DevelopmentPosHomeContextSeedConstants.DevelopmentTillActivationCodeThreeId}';

            DELETE FROM till_sessions
            WHERE id = '{DevelopmentPosHomeContextSeedConstants.DevelopmentTillSessionThreeId}';

            DELETE FROM till_device_assignments
            WHERE id = '{DevelopmentPosHomeContextSeedConstants.DevelopmentTillDeviceAssignmentThreeId}';

            DELETE FROM pos_devices
            WHERE id = '{DevelopmentPosHomeContextSeedConstants.DevelopmentPosDeviceThreeId}';

            DELETE FROM tills
            WHERE id = '{DevelopmentPosHomeContextSeedConstants.DevelopmentTillThreeId}';
            """);
    }
}
