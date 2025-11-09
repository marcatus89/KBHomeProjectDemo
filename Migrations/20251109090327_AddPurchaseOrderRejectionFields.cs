using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DoAnTotNghiep.Migrations
{
    /// <inheritdoc />
    public partial class AddPurchaseOrderRejectionFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add rejection-related columns to PurchaseOrders
            migrationBuilder.AddColumn<DateTime>(
                name: "RejectedAt",
                table: "PurchaseOrders",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RejectedByEmail",
                table: "PurchaseOrders",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RejectionReason",
                table: "PurchaseOrders",
                type: "nvarchar(max)",
                nullable: true);

            // NOTE: Description column is intentionally NOT added here because it already exists in the database
            // If you ever need to add Description from migration, use IF NOT EXISTS SQL guard.
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Remove columns we added on PurchaseOrders
            migrationBuilder.DropColumn(
                name: "RejectedAt",
                table: "PurchaseOrders");

            migrationBuilder.DropColumn(
                name: "RejectedByEmail",
                table: "PurchaseOrders");

            migrationBuilder.DropColumn(
                name: "RejectionReason",
                table: "PurchaseOrders");
        }
    }
}
