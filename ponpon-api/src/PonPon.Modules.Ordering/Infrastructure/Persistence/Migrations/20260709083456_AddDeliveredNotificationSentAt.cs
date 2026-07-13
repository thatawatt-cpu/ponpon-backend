using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PonPon.Modules.Ordering.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDeliveredNotificationSentAt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DeliveredNotificationSentAtUtc",
                schema: "ordering",
                table: "orders",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_orders_DeliveredNotificationSentAtUtc",
                schema: "ordering",
                table: "orders",
                column: "DeliveredNotificationSentAtUtc",
                filter: "\"DeliveredNotificationSentAtUtc\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_orders_DeliveredNotificationSentAtUtc",
                schema: "ordering",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "DeliveredNotificationSentAtUtc",
                schema: "ordering",
                table: "orders");
        }
    }
}
