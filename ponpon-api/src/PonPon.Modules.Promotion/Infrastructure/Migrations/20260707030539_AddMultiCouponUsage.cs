using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PonPon.Modules.Promotion.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMultiCouponUsage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_coupon_usages_OrderId",
                schema: "promotion",
                table: "coupon_usages");

            migrationBuilder.CreateIndex(
                name: "IX_coupon_usages_OrderId_CouponId",
                schema: "promotion",
                table: "coupon_usages",
                columns: new[] { "OrderId", "CouponId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_coupon_usages_OrderId_CouponId",
                schema: "promotion",
                table: "coupon_usages");

            migrationBuilder.CreateIndex(
                name: "IX_coupon_usages_OrderId",
                schema: "promotion",
                table: "coupon_usages",
                column: "OrderId",
                unique: true);
        }
    }
}
