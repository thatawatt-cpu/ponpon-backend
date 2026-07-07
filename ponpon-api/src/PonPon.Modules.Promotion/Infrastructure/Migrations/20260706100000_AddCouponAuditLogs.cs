using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PonPon.Modules.Promotion.Infrastructure.Migrations
{
    public partial class AddCouponAuditLogs : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "coupon_audit_logs",
                schema: "promotion",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CouponId = table.Column<Guid>(type: "uuid", nullable: true),
                    BatchId = table.Column<Guid>(type: "uuid", nullable: true),
                    Action = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ActorUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    ActorUserType = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    BeforeJson = table.Column<string>(type: "jsonb", nullable: true),
                    AfterJson = table.Column<string>(type: "jsonb", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_coupon_audit_logs", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_coupon_audit_logs_BatchId",
                schema: "promotion",
                table: "coupon_audit_logs",
                column: "BatchId");

            migrationBuilder.CreateIndex(
                name: "IX_coupon_audit_logs_CouponId",
                schema: "promotion",
                table: "coupon_audit_logs",
                column: "CouponId");

            migrationBuilder.CreateIndex(
                name: "IX_coupon_audit_logs_CreatedAtUtc",
                schema: "promotion",
                table: "coupon_audit_logs",
                column: "CreatedAtUtc");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "coupon_audit_logs",
                schema: "promotion");
        }
    }
}
