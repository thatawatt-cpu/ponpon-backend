using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PonPon.Modules.Catalog.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCatalogPerformanceIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_flash_sale_products_ProductId",
                schema: "catalog",
                table: "flash_sale_products",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_products_admin_list",
                schema: "catalog",
                table: "products",
                columns: new[] { "Status", "Source", "UpdatedAt", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_products_customer_category_list",
                schema: "catalog",
                table: "products",
                columns: new[] { "CategoryName", "IsActiveFromZort", "IsVisibleOnLiff", "Status", "AvailableStock", "Name" });

            migrationBuilder.CreateIndex(
                name: "IX_products_customer_list",
                schema: "catalog",
                table: "products",
                columns: new[] { "IsActiveFromZort", "IsVisibleOnLiff", "Status", "AvailableStock", "Name" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_flash_sale_products_ProductId",
                schema: "catalog",
                table: "flash_sale_products");

            migrationBuilder.DropIndex(
                name: "IX_products_admin_list",
                schema: "catalog",
                table: "products");

            migrationBuilder.DropIndex(
                name: "IX_products_customer_category_list",
                schema: "catalog",
                table: "products");

            migrationBuilder.DropIndex(
                name: "IX_products_customer_list",
                schema: "catalog",
                table: "products");
        }
    }
}
