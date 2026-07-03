using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E_POS.Infrastructure.Persistence.Migrations
{
    [DbContext(typeof(EPosDbContext))]
    [Migration("20260703101000_AddPosDeviceSearchTrigramIndexes")]
    public partial class AddPosDeviceSearchTrigramIndexes : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS pg_trgm;");
            migrationBuilder.Sql("CREATE INDEX IF NOT EXISTS ix_pos_devices_name_trgm ON pos_devices USING gin (name gin_trgm_ops);");
            migrationBuilder.Sql("CREATE INDEX IF NOT EXISTS ix_pos_devices_device_code_trgm ON pos_devices USING gin (device_code gin_trgm_ops);");
            migrationBuilder.Sql("CREATE INDEX IF NOT EXISTS ix_pos_devices_device_serial_number_trgm ON pos_devices USING gin (device_serial_number gin_trgm_ops);");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP INDEX IF EXISTS ix_pos_devices_device_serial_number_trgm;");
            migrationBuilder.Sql("DROP INDEX IF EXISTS ix_pos_devices_device_code_trgm;");
            migrationBuilder.Sql("DROP INDEX IF EXISTS ix_pos_devices_name_trgm;");
        }
    }
}