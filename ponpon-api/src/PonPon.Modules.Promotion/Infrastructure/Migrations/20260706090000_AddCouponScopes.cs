using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PonPon.Modules.Promotion.Infrastructure.Migrations
{
    public partial class AddCouponScopes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "coupon_scopes",
                schema: "promotion",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CouponId = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: true),
                    VariantId = table.Column<Guid>(type: "uuid", nullable: true),
                    Sku = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    ZortCategoryId = table.Column<long>(type: "bigint", nullable: true),
                    CategoryName = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_coupon_scopes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_coupon_scopes_coupons_CouponId",
                        column: x => x.CouponId,
                        principalSchema: "promotion",
                        principalTable: "coupons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_coupon_scopes_CouponId",
                schema: "promotion",
                table: "coupon_scopes",
                column: "CouponId");

            migrationBuilder.CreateIndex(
                name: "IX_coupon_scopes_ProductId",
                schema: "promotion",
                table: "coupon_scopes",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_coupon_scopes_Sku",
                schema: "promotion",
                table: "coupon_scopes",
                column: "Sku");

            migrationBuilder.CreateIndex(
                name: "IX_coupon_scopes_VariantId",
                schema: "promotion",
                table: "coupon_scopes",
                column: "VariantId");

            migrationBuilder.CreateIndex(
                name: "IX_coupon_scopes_ZortCategoryId",
                schema: "promotion",
                table: "coupon_scopes",
                column: "ZortCategoryId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "coupon_scopes",
                schema: "promotion");
        }
    }
}
