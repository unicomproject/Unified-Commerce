using System;
using E_POS.Infrastructure.Persistence.Seed;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E_POS.Infrastructure.Persistence.Migrations
{
    public partial class CanonicalizeCashInMovements : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "uq_cash_movement_types_tenant_id_movement_type_code",
                table: "cash_movement_types");

            migrationBuilder.AddColumn<Guid>(
                name: "request_id",
                table: "cash_movements",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "uq_cash_movements_tenant_id_request_id",
                table: "cash_movements",
                columns: new[] { "tenant_id", "request_id" },
                unique: true,
                filter: "request_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "uq_cash_movement_types_global_code",
                table: "cash_movement_types",
                column: "movement_type_code",
                unique: true,
                filter: "tenant_id IS NULL");

            migrationBuilder.CreateIndex(
                name: "uq_cash_movement_types_tenant_code",
                table: "cash_movement_types",
                columns: new[] { "tenant_id", "movement_type_code" },
                unique: true,
                filter: "tenant_id IS NOT NULL");

            migrationBuilder.Sql(CanonicalCashInMovementTypeSeedData.UpSql);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(CanonicalCashInMovementTypeSeedData.DownSql);

            migrationBuilder.DropIndex(
                name: "uq_cash_movements_tenant_id_request_id",
                table: "cash_movements");

            migrationBuilder.DropIndex(
                name: "uq_cash_movement_types_global_code",
                table: "cash_movement_types");

            migrationBuilder.DropIndex(
                name: "uq_cash_movement_types_tenant_code",
                table: "cash_movement_types");

            migrationBuilder.DropColumn(
                name: "request_id",
                table: "cash_movements");

            migrationBuilder.CreateIndex(
                name: "uq_cash_movement_types_tenant_id_movement_type_code",
                table: "cash_movement_types",
                columns: new[] { "tenant_id", "movement_type_code" },
                unique: true);
        }
    }
}
