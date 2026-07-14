using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E_POS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RestoreOutletCreateConstraints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_outlet_business_hours_outlet_id",
                table: "outlet_business_hours");

            migrationBuilder.CreateIndex(
                name: "uq_outlets_tenant_id_default_outlet",
                table: "outlets",
                column: "tenant_id",
                unique: true,
                filter: "is_default_outlet = true AND status <> 'DELETED'");

            migrationBuilder.CreateIndex(
                name: "uq_outlet_business_hours_outlet_id_day_of_week",
                table: "outlet_business_hours",
                columns: new[] { "outlet_id", "day_of_week" },
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_outlet_business_hours_open_close",
                table: "outlet_business_hours",
                sql: "is_closed = true OR (opening_time IS NOT NULL AND closing_time IS NOT NULL AND opening_time < closing_time)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "uq_outlets_tenant_id_default_outlet",
                table: "outlets");

            migrationBuilder.DropIndex(
                name: "uq_outlet_business_hours_outlet_id_day_of_week",
                table: "outlet_business_hours");

            migrationBuilder.DropCheckConstraint(
                name: "ck_outlet_business_hours_open_close",
                table: "outlet_business_hours");

            migrationBuilder.CreateIndex(
                name: "IX_outlet_business_hours_outlet_id",
                table: "outlet_business_hours",
                column: "outlet_id");
        }
    }
}
