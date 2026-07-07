using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PonPon.Modules.Shipping.Migrations
{
    /// <inheritdoc />
    public partial class AddUniqueShippingShipmentOrderId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_shipping_shipments_OrderId",
                schema: "shipping",
                table: "shipping_shipments");

            migrationBuilder.CreateIndex(
                name: "IX_shipping_shipments_OrderId",
                schema: "shipping",
                table: "shipping_shipments",
                column: "OrderId",
                unique: true,
                filter: "\"OrderId\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_shipping_shipments_OrderId",
                schema: "shipping",
                table: "shipping_shipments");

            migrationBuilder.CreateIndex(
                name: "IX_shipping_shipments_OrderId",
                schema: "shipping",
                table: "shipping_shipments",
                column: "OrderId");
        }
    }
}
