using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PonPon.Modules.Promotion.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAutoPromotions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "promotions",
                schema: "promotion",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CampaignId = table.Column<Guid>(type: "uuid", nullable: true),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    DiscountType = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    DiscountValue = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    MinimumSubtotal = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    MaximumDiscount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    StartsAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    EndsAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Timezone = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Priority = table.Column<int>(type: "integer", nullable: false),
                    CanStackWithCoupon = table.Column<bool>(type: "boolean", nullable: false),
                    CanStackWithPromotions = table.Column<bool>(type: "boolean", nullable: false),
                    CanCombineWithFlashSale = table.Column<bool>(type: "boolean", nullable: false),
                    MaximumTotalUses = table.Column<int>(type: "integer", nullable: true),
                    MaximumUsesPerCustomer = table.Column<int>(type: "integer", nullable: true),
                    UsedCount = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_promotions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_promotions_coupon_campaigns_CampaignId",
                        column: x => x.CampaignId,
                        principalSchema: "promotion",
                        principalTable: "coupon_campaigns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "promotion_conditions",
                schema: "promotion",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PromotionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Value = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_promotion_conditions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_promotion_conditions_promotions_PromotionId",
                        column: x => x.PromotionId,
                        principalSchema: "promotion",
                        principalTable: "promotions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "promotion_customer_scopes",
                schema: "promotion",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PromotionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    CustomerId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_promotion_customer_scopes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_promotion_customer_scopes_promotions_PromotionId",
                        column: x => x.PromotionId,
                        principalSchema: "promotion",
                        principalTable: "promotions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "promotion_schedule_rules",
                schema: "promotion",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PromotionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    DayOfWeek = table.Column<int>(type: "integer", nullable: true),
                    DayOfMonth = table.Column<int>(type: "integer", nullable: true),
                    StartsAtLocalTime = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                    EndsAtLocalTime = table.Column<TimeOnly>(type: "time without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_promotion_schedule_rules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_promotion_schedule_rules_promotions_PromotionId",
                        column: x => x.PromotionId,
                        principalSchema: "promotion",
                        principalTable: "promotions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "promotion_scopes",
                schema: "promotion",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PromotionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: true),
                    VariantId = table.Column<Guid>(type: "uuid", nullable: true),
                    Sku = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    CategoryName = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    IsExclude = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_promotion_scopes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_promotion_scopes_promotions_PromotionId",
                        column: x => x.PromotionId,
                        principalSchema: "promotion",
                        principalTable: "promotions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "promotion_usages",
                schema: "promotion",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PromotionId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uuid", nullable: false),
                    DiscountAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    IsReleased = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ReleasedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_promotion_usages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_promotion_usages_promotions_PromotionId",
                        column: x => x.PromotionId,
                        principalSchema: "promotion",
                        principalTable: "promotions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_promotion_conditions_PromotionId",
                schema: "promotion",
                table: "promotion_conditions",
                column: "PromotionId");

            migrationBuilder.CreateIndex(
                name: "IX_promotion_conditions_Type_Value",
                schema: "promotion",
                table: "promotion_conditions",
                columns: new[] { "Type", "Value" });

            migrationBuilder.CreateIndex(
                name: "IX_promotion_customer_scopes_CustomerId",
                schema: "promotion",
                table: "promotion_customer_scopes",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_promotion_customer_scopes_PromotionId",
                schema: "promotion",
                table: "promotion_customer_scopes",
                column: "PromotionId");

            migrationBuilder.CreateIndex(
                name: "IX_promotion_schedule_rules_PromotionId",
                schema: "promotion",
                table: "promotion_schedule_rules",
                column: "PromotionId");

            migrationBuilder.CreateIndex(
                name: "IX_promotion_schedule_rules_Type_DayOfWeek_DayOfMonth",
                schema: "promotion",
                table: "promotion_schedule_rules",
                columns: new[] { "Type", "DayOfWeek", "DayOfMonth" });

            migrationBuilder.CreateIndex(
                name: "IX_promotion_scopes_ProductId",
                schema: "promotion",
                table: "promotion_scopes",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_promotion_scopes_PromotionId",
                schema: "promotion",
                table: "promotion_scopes",
                column: "PromotionId");

            migrationBuilder.CreateIndex(
                name: "IX_promotion_scopes_Sku",
                schema: "promotion",
                table: "promotion_scopes",
                column: "Sku");

            migrationBuilder.CreateIndex(
                name: "IX_promotion_scopes_VariantId",
                schema: "promotion",
                table: "promotion_scopes",
                column: "VariantId");

            migrationBuilder.CreateIndex(
                name: "IX_promotion_usages_OrderId_PromotionId",
                schema: "promotion",
                table: "promotion_usages",
                columns: new[] { "OrderId", "PromotionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_promotion_usages_PromotionId_CustomerId_IsReleased",
                schema: "promotion",
                table: "promotion_usages",
                columns: new[] { "PromotionId", "CustomerId", "IsReleased" });

            migrationBuilder.CreateIndex(
                name: "IX_promotions_CampaignId",
                schema: "promotion",
                table: "promotions",
                column: "CampaignId");

            migrationBuilder.CreateIndex(
                name: "IX_promotions_IsActive_StartsAtUtc_EndsAtUtc_Priority",
                schema: "promotion",
                table: "promotions",
                columns: new[] { "IsActive", "StartsAtUtc", "EndsAtUtc", "Priority" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "promotion_conditions",
                schema: "promotion");

            migrationBuilder.DropTable(
                name: "promotion_customer_scopes",
                schema: "promotion");

            migrationBuilder.DropTable(
                name: "promotion_schedule_rules",
                schema: "promotion");

            migrationBuilder.DropTable(
                name: "promotion_scopes",
                schema: "promotion");

            migrationBuilder.DropTable(
                name: "promotion_usages",
                schema: "promotion");

            migrationBuilder.DropTable(
                name: "promotions",
                schema: "promotion");
        }
    }
}
