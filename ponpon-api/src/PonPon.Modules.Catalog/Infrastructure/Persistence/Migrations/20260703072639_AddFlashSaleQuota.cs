using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PonPon.Modules.Catalog.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddFlashSaleQuota : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "QuantityLimit",
                schema: "catalog",
                table: "flash_sale_products",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ReservedQuantity",
                schema: "catalog",
                table: "flash_sale_products",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "flash_sale_reservations",
                schema: "catalog",
                columns: table => new
                {
                    OrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    FlashSaleId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    IsReleased = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ReleasedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_flash_sale_reservations", x => new { x.OrderId, x.FlashSaleId, x.ProductId });
                });

            migrationBuilder.CreateIndex(
                name: "IX_flash_sale_reservations_FlashSaleId_ProductId_IsReleased",
                schema: "catalog",
                table: "flash_sale_reservations",
                columns: new[] { "FlashSaleId", "ProductId", "IsReleased" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "flash_sale_reservations",
                schema: "catalog");

            migrationBuilder.DropColumn(
                name: "QuantityLimit",
                schema: "catalog",
                table: "flash_sale_products");

            migrationBuilder.DropColumn(
                name: "ReservedQuantity",
                schema: "catalog",
                table: "flash_sale_products");
        }
    }
}
