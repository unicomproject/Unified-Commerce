using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E_POS.Infrastructure.Persistence.Migrations;

[DbContext(typeof(EPosDbContext))]
[Migration("20260810143000_CompleteTenantUserStaffCodeRollout")]
public partial class CompleteTenantUserStaffCodeRollout : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            WITH valid_existing AS (
                SELECT
                    tenant_id,
                    substring(staff_code from 5 for 4)::integer AS code_year,
                    substring(staff_code from 10 for 5)::integer AS code_value
                FROM tenant_users
                WHERE staff_code ~ '^USR-[0-9]{4}-[0-9]{5}$'
            ),
            max_existing AS (
                SELECT tenant_id, code_year, max(code_value) AS max_code_value
                FROM valid_existing
                GROUP BY tenant_id, code_year
            ),
            needs_backfill AS (
                SELECT
                    id,
                    tenant_id,
                    date_part('year', created_at AT TIME ZONE 'UTC')::integer AS code_year,
                    created_at
                FROM tenant_users
                WHERE staff_code IS NULL
                   OR btrim(staff_code) = ''
                   OR staff_code !~ '^USR-[0-9]{4}-[0-9]{5}$'
            ),
            numbered AS (
                SELECT
                    n.id,
                    n.tenant_id,
                    n.code_year,
                    coalesce(m.max_code_value, 0)
                        + row_number() OVER (
                            PARTITION BY n.tenant_id, n.code_year
                            ORDER BY n.created_at, n.id
                          ) AS next_code_value
                FROM needs_backfill n
                LEFT JOIN max_existing m
                  ON m.tenant_id = n.tenant_id
                 AND m.code_year = n.code_year
            )
            UPDATE tenant_users u
               SET staff_code = 'USR-' || numbered.code_year::text || '-' || lpad(numbered.next_code_value::text, 5, '0'),
                   updated_at = greatest(u.updated_at, u.created_at)
              FROM numbered
             WHERE u.id = numbered.id;
            """);

        migrationBuilder.Sql("""
            WITH parsed AS (
                SELECT
                    tenant_id,
                    substring(staff_code from 5 for 4)::integer AS code_year,
                    substring(staff_code from 10 for 5)::bigint AS code_value
                FROM tenant_users
                WHERE staff_code ~ '^USR-[0-9]{4}-[0-9]{5}$'
            ),
            max_codes AS (
                SELECT tenant_id, code_year, max(code_value) AS current_value
                FROM parsed
                GROUP BY tenant_id, code_year
            ),
            sequenced AS (
                SELECT
                    tenant_id,
                    code_year,
                    current_value,
                    md5('TENANT_USER_STAFF_CODE:' || tenant_id::text || ':' || code_year::text) AS hash_value
                FROM max_codes
            )
            INSERT INTO tenant_user_code_sequences (
                id,
                tenant_id,
                sequence_type,
                year,
                current_value,
                created_at,
                updated_at
            )
            SELECT
                (substring(hash_value from 1 for 8) || '-' ||
                 substring(hash_value from 9 for 4) || '-' ||
                 substring(hash_value from 13 for 4) || '-' ||
                 substring(hash_value from 17 for 4) || '-' ||
                 substring(hash_value from 21 for 12))::uuid,
                tenant_id,
                'TENANT_USER_STAFF_CODE',
                code_year,
                current_value,
                now(),
                now()
            FROM sequenced
            ON CONFLICT (tenant_id, sequence_type, year)
            DO UPDATE SET
                current_value = greatest(tenant_user_code_sequences.current_value, excluded.current_value),
                updated_at = excluded.updated_at;
            """);

        migrationBuilder.DropIndex(
            name: "uq_tenant_users_tenant_id_staff_code",
            table: "tenant_users");

        migrationBuilder.AlterColumn<string>(
            name: "staff_code",
            table: "tenant_users",
            type: "varchar(20)",
            maxLength: 20,
            nullable: false,
            oldClrType: typeof(string),
            oldType: "varchar(20)",
            oldMaxLength: 20,
            oldNullable: true);

        migrationBuilder.CreateIndex(
            name: "uq_tenant_users_tenant_id_staff_code",
            table: "tenant_users",
            columns: new[] { "tenant_id", "staff_code" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ix_tenant_user_invite_delivery_secrets_cleanup",
            table: "tenant_user_invite_delivery_secrets",
            columns: new[] { "purged_at", "expires_at", "created_at" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "ix_tenant_user_invite_delivery_secrets_cleanup",
            table: "tenant_user_invite_delivery_secrets");

        migrationBuilder.DropIndex(
            name: "uq_tenant_users_tenant_id_staff_code",
            table: "tenant_users");

        migrationBuilder.AlterColumn<string>(
            name: "staff_code",
            table: "tenant_users",
            type: "varchar(20)",
            maxLength: 20,
            nullable: true,
            oldClrType: typeof(string),
            oldType: "varchar(20)",
            oldMaxLength: 20);

        migrationBuilder.CreateIndex(
            name: "uq_tenant_users_tenant_id_staff_code",
            table: "tenant_users",
            columns: new[] { "tenant_id", "staff_code" },
            unique: true,
            filter: "staff_code IS NOT NULL");
    }
}
