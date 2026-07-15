using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PonPon.Modules.Promotion.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCouponSoftDelete : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                schema: "promotion",
                table: "coupons",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_coupons_IsDeleted",
                schema: "promotion",
                table: "coupons",
                column: "IsDeleted");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_coupons_IsDeleted",
                schema: "promotion",
                table: "coupons");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                schema: "promotion",
                table: "coupons");
        }
    }
}
