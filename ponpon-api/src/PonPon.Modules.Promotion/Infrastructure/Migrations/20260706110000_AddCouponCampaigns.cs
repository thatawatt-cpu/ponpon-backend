using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PonPon.Modules.Promotion.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCouponCampaigns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CampaignId",
                schema: "promotion",
                table: "coupons",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "DiscountAmount",
                schema: "promotion",
                table: "coupon_usages",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateTable(
                name: "coupon_campaigns",
                schema: "promotion",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    StartsAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    EndsAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_coupon_campaigns", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_coupons_CampaignId",
                schema: "promotion",
                table: "coupons",
                column: "CampaignId");

            migrationBuilder.CreateIndex(
                name: "IX_coupon_campaigns_IsActive_StartsAtUtc_EndsAtUtc",
                schema: "promotion",
                table: "coupon_campaigns",
                columns: new[] { "IsActive", "StartsAtUtc", "EndsAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_coupon_campaigns_Name",
                schema: "promotion",
                table: "coupon_campaigns",
                column: "Name");

            migrationBuilder.AddForeignKey(
                name: "FK_coupons_coupon_campaigns_CampaignId",
                schema: "promotion",
                table: "coupons",
                column: "CampaignId",
                principalSchema: "promotion",
                principalTable: "coupon_campaigns",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_coupons_coupon_campaigns_CampaignId",
                schema: "promotion",
                table: "coupons");

            migrationBuilder.DropTable(
                name: "coupon_campaigns",
                schema: "promotion");

            migrationBuilder.DropIndex(
                name: "IX_coupons_CampaignId",
                schema: "promotion",
                table: "coupons");

            migrationBuilder.DropColumn(
                name: "CampaignId",
                schema: "promotion",
                table: "coupons");

            migrationBuilder.DropColumn(
                name: "DiscountAmount",
                schema: "promotion",
                table: "coupon_usages");
        }
    }
}
