using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PonPon.Modules.Promotion.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCouponBulkGenerationJobs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "coupon_bulk_generation_jobs",
                schema: "promotion",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BatchId = table.Column<Guid>(type: "uuid", nullable: false),
                    CampaignId = table.Column<Guid>(type: "uuid", nullable: true),
                    Prefix = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    RequestedCount = table.Column<int>(type: "integer", nullable: false),
                    CreatedCount = table.Column<int>(type: "integer", nullable: false),
                    InputJson = table.Column<string>(type: "jsonb", nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    BackgroundJobId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    Error = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    RequestedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    RequestedByUserType = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    RequestedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    StartedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CompletedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_coupon_bulk_generation_jobs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_coupon_bulk_generation_jobs_coupon_campaigns_CampaignId",
                        column: x => x.CampaignId,
                        principalSchema: "promotion",
                        principalTable: "coupon_campaigns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_coupon_bulk_generation_jobs_BatchId",
                schema: "promotion",
                table: "coupon_bulk_generation_jobs",
                column: "BatchId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_coupon_bulk_generation_jobs_CampaignId",
                schema: "promotion",
                table: "coupon_bulk_generation_jobs",
                column: "CampaignId");

            migrationBuilder.CreateIndex(
                name: "IX_coupon_bulk_generation_jobs_Status_RequestedAtUtc",
                schema: "promotion",
                table: "coupon_bulk_generation_jobs",
                columns: new[] { "Status", "RequestedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "coupon_bulk_generation_jobs",
                schema: "promotion");
        }
    }
}
