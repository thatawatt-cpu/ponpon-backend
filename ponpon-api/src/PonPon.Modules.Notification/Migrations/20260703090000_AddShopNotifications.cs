using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PonPon.Modules.Notification.Migrations
{
    [Migration("20260703090000_AddShopNotifications")]
    public partial class AddShopNotifications : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(name: "notification");

            migrationBuilder.CreateTable(
                name: "shop_notifications",
                schema: "notification",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uuid", nullable: true),
                    LineUserId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    Type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    OrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderNumber = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Message = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    Status = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    TrackingNumber = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    ActionUrl = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ReadAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_shop_notifications", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_shop_notifications_CreatedAtUtc",
                schema: "notification",
                table: "shop_notifications",
                column: "CreatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_shop_notifications_CustomerId",
                schema: "notification",
                table: "shop_notifications",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_shop_notifications_LineUserId",
                schema: "notification",
                table: "shop_notifications",
                column: "LineUserId");

            migrationBuilder.CreateIndex(
                name: "IX_shop_notifications_OrderId",
                schema: "notification",
                table: "shop_notifications",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_shop_notifications_ReadAtUtc",
                schema: "notification",
                table: "shop_notifications",
                column: "ReadAtUtc");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "shop_notifications",
                schema: "notification");
        }
    }
}
