using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E_POS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class FixOneVerzPrinterConnectionType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Data-only repair: a RECEIPT_PRINTER row catalogued through Tenant Admin
            // with connection_type=USB and a declared compatibilityProfileId permanently
            // blocks the POS app's own Settings screen from saving its Local Print Agent
            // configuration for the same device (the app only ever sends
            // transportType=localPrintAgent). PosHardwareRepository.SaveConfigurationAsync
            // treats any transport mismatch against an already-profiled device as
            // "pos_hardware.assignment_mismatch" ("The outlet or till assignment does not
            // match this POS device."), which is misleading here — the real conflict is a
            // stale catalogue entry, not an outlet/till problem. This retargets that one
            // device to LOCALPRINTAGENT with sensible defaults so the next save from the
            // app succeeds; the operator still enters the Local Print Agent API key in the
            // app (it is never stored server-side).
            migrationBuilder.Sql(@"
                UPDATE hardware_devices
                SET connection_type = 'LOCALPRINTAGENT',
                    config_json = '{""agentBaseUrl"":""http://localhost:9101"",""printerName"":""OneVerz POS-80"",""paperWidth"":""80mm"",""autoCut"":true,""requestTimeout"":8000,""feedBeforeCut"":5,""localApiKeyPresent"":false,""printCustomerCopy"":true,""customerCopyCount"":1,""printMerchantCopy"":false,""merchantCopyCount"":0,""networkPort"":9100}',
                    configuration_version = configuration_version + 1,
                    updated_at = now()
                WHERE hardware_device_type = 'RECEIPT_PRINTER'
                  AND hardware_device_name = 'OneVerz POS-80'
                  AND connection_type = 'USB';
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Irreversible data repair; the prior USB catalogue entry is not
            // reconstructed. A fresh Tenant Admin re-catalogue would be required
            // to restore it.
        }
    }
}
