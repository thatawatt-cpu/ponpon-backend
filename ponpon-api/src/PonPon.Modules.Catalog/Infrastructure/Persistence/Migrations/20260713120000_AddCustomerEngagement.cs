using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PonPon.Modules.Catalog.Infrastructure.Persistence.Migrations
{
    public partial class AddCustomerEngagement : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "customer_recently_viewed_products",
                schema: "catalog",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    ViewedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_customer_recently_viewed_products", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "customer_wishlist_items",
                schema: "catalog",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_customer_wishlist_items", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_customer_recently_viewed_products_CustomerId_ProductId",
                schema: "catalog",
                table: "customer_recently_viewed_products",
                columns: new[] { "CustomerId", "ProductId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_customer_recently_viewed_products_CustomerId_ViewedAtUtc",
                schema: "catalog",
                table: "customer_recently_viewed_products",
                columns: new[] { "CustomerId", "ViewedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_customer_wishlist_items_CustomerId_CreatedAtUtc",
                schema: "catalog",
                table: "customer_wishlist_items",
                columns: new[] { "CustomerId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_customer_wishlist_items_CustomerId_ProductId",
                schema: "catalog",
                table: "customer_wishlist_items",
                columns: new[] { "CustomerId", "ProductId" },
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "customer_recently_viewed_products",
                schema: "catalog");

            migrationBuilder.DropTable(
                name: "customer_wishlist_items",
                schema: "catalog");
        }
    }
}
