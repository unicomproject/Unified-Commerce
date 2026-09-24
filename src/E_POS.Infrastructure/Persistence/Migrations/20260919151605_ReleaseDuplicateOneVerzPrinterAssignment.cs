using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E_POS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ReleaseDuplicateOneVerzPrinterAssignment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Data-only repair: two separate RECEIPT_PRINTER hardware_devices rows
            // (both display-named "OneVerz POS-80") are both actively assigned to the
            // same till. PosHardwareRepository.SaveConfigurationAsync treats more than
            // one matching candidate as ambiguous and refuses to save
            // ("pos_hardware.assignment_mismatch" / "The outlet or till assignment does
            // not match this POS device."), regardless of connection_type, which is a
            // second, independent reason the POS app's own Settings screen could never
            // save its Local Print Agent configuration for this printer. This releases
            // the assignment for device ace33ee3-59bd-44e5-aba8-f3fbc93d56cf, leaving
            // d6b5a93a-7aca-4ee2-8046-27a5cea15d6b (the Xprinter XP-80T device with a
            // real captured printerQueue identity from prior Windows enumeration) as the
            // sole active printer assignment for this till. The device row itself is not
            // deleted, only its assignment to the till is released.
            migrationBuilder.Sql(@"
                UPDATE hardware_device_assignments
                SET released_at = now(),
                    release_reason = 'Duplicate RECEIPT_PRINTER assignment on the same till; consolidated to the device with a captured physical identity.'
                WHERE id = '08ebdd1b-e86d-4132-b1ed-6b25b7dd7604'
                  AND hardware_device_id = 'ace33ee3-59bd-44e5-aba8-f3fbc93d56cf'
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
                WHERE id = '08ebdd1b-e86d-4132-b1ed-6b25b7dd7604'
                  AND hardware_device_id = 'ace33ee3-59bd-44e5-aba8-f3fbc93d56cf';
            ");
        }
    }
}
