using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E_POS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSalesChannelIdToTenantDomain : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "sales_channel_id",
                table: "tenant_domains",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_tenant_domains_sales_channel_id",
                table: "tenant_domains",
                column: "sales_channel_id");

            migrationBuilder.AddForeignKey(
                name: "fk_tenant_domains_sales_channel_id_sales_channels",
                table: "tenant_domains",
                column: "sales_channel_id",
                principalTable: "sales_channels",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_tenant_domains_sales_channel_id_sales_channels",
                table: "tenant_domains");

            migrationBuilder.DropIndex(
                name: "ix_tenant_domains_sales_channel_id",
                table: "tenant_domains");

            migrationBuilder.DropColumn(
                name: "sales_channel_id",
                table: "tenant_domains");
        }
    }
}
