using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PonPon.Modules.Promotion.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCouponClaims : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "coupon_claims",
                schema: "promotion",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CouponId = table.Column<Guid>(type: "uuid", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClaimedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_coupon_claims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_coupon_claims_coupons_CouponId",
                        column: x => x.CouponId,
                        principalSchema: "promotion",
                        principalTable: "coupons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_coupon_claims_ClaimedAtUtc",
                schema: "promotion",
                table: "coupon_claims",
                column: "ClaimedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_coupon_claims_CouponId_CustomerId",
                schema: "promotion",
                table: "coupon_claims",
                columns: new[] { "CouponId", "CustomerId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_coupon_claims_CustomerId",
                schema: "promotion",
                table: "coupon_claims",
                column: "CustomerId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "coupon_claims",
                schema: "promotion");
        }
    }
}
