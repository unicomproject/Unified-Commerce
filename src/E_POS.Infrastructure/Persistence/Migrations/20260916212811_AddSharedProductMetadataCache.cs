using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E_POS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSharedProductMetadataCache : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "shared_product_metadata_cache",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    normalized_barcode = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: false),
                    identifier_standard = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: true),
                    provider = table.Column<string>(type: "varchar(60)", maxLength: 60, nullable: false),
                    normalized_metadata_json = table.Column<string>(type: "jsonb", nullable: false),
                    raw_response_json = table.Column<string>(type: "jsonb", nullable: true),
                    cached_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    last_verified_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_shared_product_metadata_cache", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_shared_product_metadata_cache_barcode_expires",
                table: "shared_product_metadata_cache",
                columns: new[] { "normalized_barcode", "expires_at" });

            migrationBuilder.CreateIndex(
                name: "uq_shared_product_metadata_cache_barcode_provider",
                table: "shared_product_metadata_cache",
                columns: new[] { "normalized_barcode", "provider" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "shared_product_metadata_cache");
        }
    }
}
