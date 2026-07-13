using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PonPon.Modules.Ordering.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderingPerformanceIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_order_items_ProductId_OrderId",
                schema: "ordering",
                table: "order_items",
                columns: new[] { "ProductId", "OrderId" });

            migrationBuilder.CreateIndex(
                name: "IX_orders_customer_order_date",
                schema: "ordering",
                table: "orders",
                columns: new[] { "CustomerId", "OrderDate" });

            migrationBuilder.CreateIndex(
                name: "IX_orders_customer_payment_status",
                schema: "ordering",
                table: "orders",
                columns: new[] { "CustomerId", "PaymentStatus", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_order_items_ProductId_OrderId",
                schema: "ordering",
                table: "order_items");

            migrationBuilder.DropIndex(
                name: "IX_orders_customer_order_date",
                schema: "ordering",
                table: "orders");

            migrationBuilder.DropIndex(
                name: "IX_orders_customer_payment_status",
                schema: "ordering",
                table: "orders");
        }
    }
}
