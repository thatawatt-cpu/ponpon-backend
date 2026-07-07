using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PonPon.Modules.Promotion.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCouponStackingRules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "CanStackWithCoupons",
                schema: "promotion",
                table: "coupons",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "CanStackWithPromotions",
                schema: "promotion",
                table: "coupons",
                type: "boolean",
                nullable: false,
                defaultValue: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CanStackWithCoupons",
                schema: "promotion",
                table: "coupons");

            migrationBuilder.DropColumn(
                name: "CanStackWithPromotions",
                schema: "promotion",
                table: "coupons");
        }
    }
}
