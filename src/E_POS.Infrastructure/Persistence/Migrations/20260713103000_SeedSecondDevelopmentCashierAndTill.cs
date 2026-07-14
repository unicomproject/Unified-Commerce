using E_POS.Application.Common.Security;
using E_POS.Infrastructure.Persistence.Seed;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E_POS.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class SeedSecondDevelopmentCashierAndTill : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        const string passwordHash = "PBKDF2-SHA256:100000:zG7O+AY1EJBG5+sCXDBinA==:weI+nABmBRNW19gQODOHn5D2q8SUQ0rVJy0NITO/Qyo=";
        const string passwordSalt = "dev_seed";
        var activationCodeHash = DeviceFingerprintHasher.Hash(
            DevelopmentPosHomeContextSeedConstants.DevelopmentActivationCodeTwo);

        migrationBuilder.Sql($"""
            INSERT INTO tenant_users (
                id, tenant_id, email, encrypted_password, phone, unmasked_phone,
                password_salt, full_name, display_name, outlet_id, default_outlet_id,
                user_type, account_status, failed_login_attempts, accepted_privacy_terms,
                accepted_terms_version, source_user_type, notes, created_at, updated_at
            )
            VALUES (
                '{DevelopmentTenantSeedConstants.CashierTwoUserId}',
                '{DevelopmentTenantSeedConstants.DevelopmentTenantId}',
                '{DevelopmentTenantSeedConstants.CashierTwoEmail}',
                '{passwordHash}',
                NULL,
                NULL,
                '{passwordSalt}',
                'Development Cashier 002',
                'Cashier 002',
                '{DevelopmentPosHomeContextSeedConstants.DevelopmentOutletId}',
                'DEV-STORE-01',
                'outlet',
                'ACTIVE',
                0,
                true,
                '1.0',
                'outlet',
                'Development seed cashier for second till.',
                now(),
                now()
            )
            ON CONFLICT (tenant_id, email) DO UPDATE
            SET encrypted_password = EXCLUDED.encrypted_password,
                password_salt = EXCLUDED.password_salt,
                full_name = EXCLUDED.full_name,
                display_name = EXCLUDED.display_name,
                outlet_id = EXCLUDED.outlet_id,
                default_outlet_id = EXCLUDED.default_outlet_id,
                user_type = EXCLUDED.user_type,
                account_status = 'ACTIVE',
                source_user_type = EXCLUDED.source_user_type,
                notes = EXCLUDED.notes,
                updated_at = now();

            INSERT INTO tenant_user_roles (
                id, tenant_id, user_id, role_id, assigned_by_tenant_user_id,
                assigned_at, revoked_at, created_at
            )
            VALUES (
                'aaaaaaaa-0006-4000-8000-000000000001',
                '{DevelopmentTenantSeedConstants.DevelopmentTenantId}',
                '{DevelopmentTenantSeedConstants.CashierTwoUserId}',
                '{DevelopmentTenantSeedConstants.CashierRoleId}',
                NULL,
                now(),
                NULL,
                now()
            )
            ON CONFLICT (tenant_id, user_id, role_id) DO UPDATE
            SET revoked_at = NULL,
                assigned_at = COALESCE(tenant_user_roles.assigned_at, EXCLUDED.assigned_at);

            INSERT INTO tills (
                id, tenant_id, outlet_id, till_area_name, till_number, till_name, till_code,
                till_type, default_opening_float_amount, currency_code, is_cash_managed, status,
                created_by_tenant_user_id, updated_by_tenant_user_id, created_at, updated_at
            )
            VALUES (
                '{DevelopmentPosHomeContextSeedConstants.DevelopmentTillTwoId}',
                '{DevelopmentTenantSeedConstants.DevelopmentTenantId}',
                '{DevelopmentPosHomeContextSeedConstants.DevelopmentOutletId}',
                'Front',
                2,
                'Front Till 02',
                'FRONT-02',
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
                '{DevelopmentPosHomeContextSeedConstants.DevelopmentPosDeviceTwoId}',
                '{DevelopmentTenantSeedConstants.DevelopmentTenantId}',
                '{DevelopmentPosHomeContextSeedConstants.DevelopmentOutletId}',
                'Front POS Device 02',
                'POS-02',
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
                '{DevelopmentPosHomeContextSeedConstants.DevelopmentTillDeviceAssignmentTwoId}',
                '{DevelopmentTenantSeedConstants.DevelopmentTenantId}',
                '{DevelopmentPosHomeContextSeedConstants.DevelopmentOutletId}',
                '{DevelopmentPosHomeContextSeedConstants.DevelopmentTillTwoId}',
                '{DevelopmentPosHomeContextSeedConstants.DevelopmentPosDeviceTwoId}',
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
                '{DevelopmentPosHomeContextSeedConstants.DevelopmentTillSessionTwoId}',
                '{DevelopmentTenantSeedConstants.DevelopmentTenantId}',
                '{DevelopmentPosHomeContextSeedConstants.DevelopmentOutletId}',
                '{DevelopmentPosHomeContextSeedConstants.DevelopmentTillTwoId}',
                'TS-DEV-0002',
                CURRENT_DATE,
                '{DevelopmentTenantSeedConstants.CashierTwoUserId}',
                '{DevelopmentPosHomeContextSeedConstants.DevelopmentPosDeviceTwoId}',
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
                '{DevelopmentPosHomeContextSeedConstants.DevelopmentTillActivationCodeTwoId}',
                '{DevelopmentTenantSeedConstants.DevelopmentTenantId}',
                '{DevelopmentPosHomeContextSeedConstants.DevelopmentOutletId}',
                '{DevelopmentPosHomeContextSeedConstants.DevelopmentTillTwoId}',
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
            WHERE id = '{DevelopmentPosHomeContextSeedConstants.DevelopmentTillActivationCodeTwoId}';

            DELETE FROM till_sessions
            WHERE id = '{DevelopmentPosHomeContextSeedConstants.DevelopmentTillSessionTwoId}';

            DELETE FROM till_device_assignments
            WHERE id = '{DevelopmentPosHomeContextSeedConstants.DevelopmentTillDeviceAssignmentTwoId}';

            DELETE FROM pos_devices
            WHERE id = '{DevelopmentPosHomeContextSeedConstants.DevelopmentPosDeviceTwoId}';

            DELETE FROM tills
            WHERE id = '{DevelopmentPosHomeContextSeedConstants.DevelopmentTillTwoId}';

            DELETE FROM tenant_user_roles
            WHERE tenant_id = '{DevelopmentTenantSeedConstants.DevelopmentTenantId}'
              AND user_id = '{DevelopmentTenantSeedConstants.CashierTwoUserId}'
              AND role_id = '{DevelopmentTenantSeedConstants.CashierRoleId}';

            DELETE FROM tenant_users
            WHERE id = '{DevelopmentTenantSeedConstants.CashierTwoUserId}';
            """);
    }
}
