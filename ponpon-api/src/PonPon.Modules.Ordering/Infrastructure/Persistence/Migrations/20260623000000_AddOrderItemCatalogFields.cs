using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PonPon.Modules.Ordering.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderItemCatalogFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ProductId",
                schema: "ordering",
                table: "order_items",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "VariantId",
                schema: "ordering",
                table: "order_items",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ImageUrl",
                schema: "ordering",
                table: "order_items",
                type: "character varying(2048)",
                maxLength: 2048,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OptionsJson",
                schema: "ordering",
                table: "order_items",
                type: "jsonb",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_order_items_ProductId",
                schema: "ordering",
                table: "order_items",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_order_items_VariantId",
                schema: "ordering",
                table: "order_items",
                column: "VariantId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_order_items_ProductId",
                schema: "ordering",
                table: "order_items");

            migrationBuilder.DropIndex(
                name: "IX_order_items_VariantId",
                schema: "ordering",
                table: "order_items");

            migrationBuilder.DropColumn(
                name: "ProductId",
                schema: "ordering",
                table: "order_items");

            migrationBuilder.DropColumn(
                name: "VariantId",
                schema: "ordering",
                table: "order_items");

            migrationBuilder.DropColumn(
                name: "ImageUrl",
                schema: "ordering",
                table: "order_items");

            migrationBuilder.DropColumn(
                name: "OptionsJson",
                schema: "ordering",
                table: "order_items");
        }
    }
}
