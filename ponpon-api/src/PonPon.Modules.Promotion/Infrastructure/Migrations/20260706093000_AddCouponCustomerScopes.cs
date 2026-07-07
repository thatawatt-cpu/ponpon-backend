using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PonPon.Modules.Promotion.Infrastructure.Migrations
{
    public partial class AddCouponCustomerScopes : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "coupon_customer_scopes",
                schema: "promotion",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CouponId = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    CustomerId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_coupon_customer_scopes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_coupon_customer_scopes_coupons_CouponId",
                        column: x => x.CouponId,
                        principalSchema: "promotion",
                        principalTable: "coupons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_coupon_customer_scopes_CouponId",
                schema: "promotion",
                table: "coupon_customer_scopes",
                column: "CouponId");

            migrationBuilder.CreateIndex(
                name: "IX_coupon_customer_scopes_CustomerId",
                schema: "promotion",
                table: "coupon_customer_scopes",
                column: "CustomerId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "coupon_customer_scopes",
                schema: "promotion");
        }
    }
}
