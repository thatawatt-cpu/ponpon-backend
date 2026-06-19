using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PonPon.Modules.Catalog.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddProductPonPonFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Highlights",
                schema: "catalog",
                table: "products",
                type: "character varying(2048)",
                maxLength: 2048,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsBestSeller",
                schema: "catalog",
                table: "products",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsFeatured",
                schema: "catalog",
                table: "products",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsOnHomepage",
                schema: "catalog",
                table: "products",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "OriginalPrice",
                schema: "catalog",
                table: "products",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PromotionBadge",
                schema: "catalog",
                table: "products",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RichDescription",
                schema: "catalog",
                table: "products",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Slug",
                schema: "catalog",
                table: "products",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_products_Slug",
                schema: "catalog",
                table: "products",
                column: "Slug",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_products_Slug",
                schema: "catalog",
                table: "products");

            migrationBuilder.DropColumn(
                name: "Highlights",
                schema: "catalog",
                table: "products");

            migrationBuilder.DropColumn(
                name: "IsBestSeller",
                schema: "catalog",
                table: "products");

            migrationBuilder.DropColumn(
                name: "IsFeatured",
                schema: "catalog",
                table: "products");

            migrationBuilder.DropColumn(
                name: "IsOnHomepage",
                schema: "catalog",
                table: "products");

            migrationBuilder.DropColumn(
                name: "OriginalPrice",
                schema: "catalog",
                table: "products");

            migrationBuilder.DropColumn(
                name: "PromotionBadge",
                schema: "catalog",
                table: "products");

            migrationBuilder.DropColumn(
                name: "RichDescription",
                schema: "catalog",
                table: "products");

            migrationBuilder.DropColumn(
                name: "Slug",
                schema: "catalog",
                table: "products");
        }
    }
}
