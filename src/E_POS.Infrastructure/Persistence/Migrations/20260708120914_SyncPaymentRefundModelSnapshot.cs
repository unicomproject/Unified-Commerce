using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E_POS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SyncPaymentRefundModelSnapshot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Snapshot-only migration. The database already matches the sales_*
            // payment/refund model (created by earlier migrations up to
            // PaymentRefundRefactoring). This migration only realigns the EF model
            // snapshot after the domain/config code was restored to that model.
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
