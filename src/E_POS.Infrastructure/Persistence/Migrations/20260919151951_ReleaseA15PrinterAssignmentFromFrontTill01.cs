using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E_POS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ReleaseA15PrinterAssignmentFromFrontTill01 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Continuation of 20260919151605_ReleaseDuplicateOneVerzPrinterAssignment:
            // a THIRD RECEIPT_PRINTER device ("A15", hardware_devices id
            // 0a1101ed-921b-40e0-af80-fbf9fa66bdc8, connection_type USB) is also
            // actively assigned to the same till (bbbbbbbb-0002-4000-8000-000000000001),
            // which still leaves 2 active RECEIPT_PRINTER candidates for that till and
            // still trips PosHardwareRepository.SaveConfigurationAsync's
            // "more than one candidate" ambiguity guard even after the prior migration.
            // Release this assignment too, leaving only
            // d6b5a93a-7aca-4ee2-8046-27a5cea15d6b (OneVerz POS-80, LOCALPRINTAGENT,
            // with a captured physical printerQueue identity) as the till's sole active
            // printer. The A15 device row itself is untouched, only its till assignment.
            migrationBuilder.Sql(@"
                UPDATE hardware_device_assignments
                SET released_at = now(),
                    release_reason = 'Duplicate RECEIPT_PRINTER assignment on the same till; consolidated to the device with a captured physical identity.'
                WHERE id = '5eb623e6-a7c6-4a96-860b-36345bc67000'
                  AND hardware_device_id = '0a1101ed-921b-40e0-af80-fbf9fa66bdc8'
                  AND released_at IS NULL;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                UPDATE hardware_device_assignments
                SET released_at = NULL,
                    release_reason = NULL
                WHERE id = '5eb623e6-a7c6-4a96-860b-36345bc67000'
                  AND hardware_device_id = '0a1101ed-921b-40e0-af80-fbf9fa66bdc8';
            ");
        }
    }
}
