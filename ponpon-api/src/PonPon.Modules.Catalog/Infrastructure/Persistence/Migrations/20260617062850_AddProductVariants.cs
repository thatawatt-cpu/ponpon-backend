using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PonPon.Modules.Catalog.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddProductVariants : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_products_ZortProductId",
                schema: "catalog",
                table: "products");

            migrationBuilder.AddColumn<string>(
                name: "BaseSku",
                schema: "catalog",
                table: "products",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "product_variants",
                schema: "catalog",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    ZortProductId = table.Column<long>(type: "bigint", nullable: true),
                    ZortVariationId = table.Column<long>(type: "bigint", nullable: true),
                    BaseSku = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Sku = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    VariantCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    Barcode = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    SellPrice = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    SellVatStatus = table.Column<int>(type: "integer", nullable: false),
                    PurchasePrice = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    PurchaseVatStatus = table.Column<int>(type: "integer", nullable: false),
                    Stock = table.Column<int>(type: "integer", nullable: false),
                    AvailableStock = table.Column<int>(type: "integer", nullable: false),
                    UnitText = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    ImageUrl = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    IsActiveFromZort = table.Column<bool>(type: "boolean", nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    RawZortJson = table.Column<string>(type: "jsonb", nullable: true),
                    LastSyncedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_product_variants", x => x.Id);
                    table.ForeignKey(
                        name: "FK_product_variants_products_ProductId",
                        column: x => x.ProductId,
                        principalSchema: "catalog",
                        principalTable: "products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_products_BaseSku",
                schema: "catalog",
                table: "products",
                column: "BaseSku");

            migrationBuilder.CreateIndex(
                name: "IX_products_ZortProductId",
                schema: "catalog",
                table: "products",
                column: "ZortProductId");

            migrationBuilder.CreateIndex(
                name: "IX_product_variants_ProductId",
                schema: "catalog",
                table: "product_variants",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_product_variants_ProductId_VariantCode",
                schema: "catalog",
                table: "product_variants",
                columns: new[] { "ProductId", "VariantCode" });

            migrationBuilder.CreateIndex(
                name: "IX_product_variants_Sku",
                schema: "catalog",
                table: "product_variants",
                column: "Sku",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_product_variants_ZortProductId",
                schema: "catalog",
                table: "product_variants",
                column: "ZortProductId",
                unique: true);

            migrationBuilder.Sql("""
                UPDATE catalog.products
                SET "BaseSku" = CASE
                    WHEN position('-' in "Sku") > 0 THEN split_part("Sku", '-', 1)
                    ELSE "Sku"
                END
                WHERE "BaseSku" IS NULL;
                """);

            migrationBuilder.Sql("""
                INSERT INTO catalog.product_variants (
                    "Id",
                    "ProductId",
                    "ZortProductId",
                    "ZortVariationId",
                    "BaseSku",
                    "Sku",
                    "VariantCode",
                    "Barcode",
                    "SellPrice",
                    "SellVatStatus",
                    "PurchasePrice",
                    "PurchaseVatStatus",
                    "Stock",
                    "AvailableStock",
                    "UnitText",
                    "ImageUrl",
                    "IsActiveFromZort",
                    "Status",
                    "RawZortJson",
                    "LastSyncedAt",
                    "CreatedAt",
                    "UpdatedAt"
                )
                SELECT
                    gen_random_uuid(),
                    "Id",
                    "ZortProductId",
                    "ZortVariationId",
                    "BaseSku",
                    "Sku",
                    CASE
                        WHEN position('-' in "Sku") > 0 THEN split_part("Sku", '-', 2)
                        ELSE NULL
                    END,
                    "Barcode",
                    "SellPrice",
                    "SellVatStatus",
                    "PurchasePrice",
                    "PurchaseVatStatus",
                    "Stock",
                    "AvailableStock",
                    "UnitText",
                    "ImageUrl",
                    "IsActiveFromZort",
                    "Status",
                    "RawZortJson",
                    "LastSyncedAt",
                    "CreatedAt",
                    "UpdatedAt"
                FROM catalog.products
                ON CONFLICT DO NOTHING;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "product_variants",
                schema: "catalog");

            migrationBuilder.DropIndex(
                name: "IX_products_BaseSku",
                schema: "catalog",
                table: "products");

            migrationBuilder.DropIndex(
                name: "IX_products_ZortProductId",
                schema: "catalog",
                table: "products");

            migrationBuilder.DropColumn(
                name: "BaseSku",
                schema: "catalog",
                table: "products");

            migrationBuilder.CreateIndex(
                name: "IX_products_ZortProductId",
                schema: "catalog",
                table: "products",
                column: "ZortProductId",
                unique: true);
        }
    }
}
