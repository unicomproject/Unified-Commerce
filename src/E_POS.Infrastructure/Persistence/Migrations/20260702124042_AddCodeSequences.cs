using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E_POS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCodeSequences : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "code_sequences",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sequence_key = table.Column<string>(type: "varchar(160)", maxLength: 160, nullable: false),
                    prefix = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    current_value = table.Column<int>(type: "integer", nullable: false),
                    padding_length = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_code_sequences", x => x.id);
                    table.CheckConstraint("ck_code_sequences_current_value", "current_value >= 0");
                    table.CheckConstraint("ck_code_sequences_padding_length", "padding_length > 0");
                    table.ForeignKey(
                        name: "fk_code_sequences_tenant_id_tenants",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "uq_code_sequences_tenant_id_sequence_key",
                table: "code_sequences",
                columns: new[] { "tenant_id", "sequence_key" },
                unique: true);

            migrationBuilder.Sql("""
                INSERT INTO code_sequences (id, tenant_id, sequence_key, prefix, current_value, padding_length, created_at, updated_at)
                SELECT md5(tenant_id::text || ':OUTLET_CODE')::uuid,
                       tenant_id,
                       'OUTLET_CODE',
                       'OUT',
                       MAX(substring(outlet_code from '^OUT([0-9]+)$')::integer),
                       3,
                       now(),
                       now()
                FROM outlets
                WHERE outlet_code ~ '^OUT[0-9]+$'
                GROUP BY tenant_id
                ON CONFLICT (tenant_id, sequence_key) DO UPDATE
                SET current_value = GREATEST(code_sequences.current_value, EXCLUDED.current_value),
                    prefix = EXCLUDED.prefix,
                    padding_length = EXCLUDED.padding_length,
                    updated_at = now();
                """);

            migrationBuilder.Sql("""
                INSERT INTO code_sequences (id, tenant_id, sequence_key, prefix, current_value, padding_length, created_at, updated_at)
                SELECT md5(tenant_id::text || ':TILL_CODE:' || outlet_id::text)::uuid,
                       tenant_id,
                       'TILL_CODE:' || replace(outlet_id::text, '-', ''),
                       'TILL',
                       MAX(substring(till_code from '^TILL([0-9]+)$')::integer),
                       3,
                       now(),
                       now()
                FROM tills
                WHERE outlet_id IS NOT NULL
                  AND till_code ~ '^TILL[0-9]+$'
                GROUP BY tenant_id, outlet_id
                ON CONFLICT (tenant_id, sequence_key) DO UPDATE
                SET current_value = GREATEST(code_sequences.current_value, EXCLUDED.current_value),
                    prefix = EXCLUDED.prefix,
                    padding_length = EXCLUDED.padding_length,
                    updated_at = now();
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "code_sequences");
        }
    }
}