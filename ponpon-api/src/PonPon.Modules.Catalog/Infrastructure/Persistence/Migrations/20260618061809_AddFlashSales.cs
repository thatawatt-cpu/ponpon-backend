using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PonPon.Modules.Catalog.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddFlashSales : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "flash_sales",
                schema: "catalog",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Slots = table.Column<string[]>(type: "text[]", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_flash_sales", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "flash_sale_products",
                schema: "catalog",
                columns: table => new
                {
                    FlashSaleId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    SalePrice = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_flash_sale_products", x => new { x.FlashSaleId, x.ProductId });
                    table.ForeignKey(
                        name: "FK_flash_sale_products_flash_sales_FlashSaleId",
                        column: x => x.FlashSaleId,
                        principalSchema: "catalog",
                        principalTable: "flash_sales",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "flash_sale_products",
                schema: "catalog");

            migrationBuilder.DropTable(
                name: "flash_sales",
                schema: "catalog");
        }
    }
}
