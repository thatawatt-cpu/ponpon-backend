using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PonPon.Modules.Ordering.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDashboardDateExpressionIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                CREATE INDEX IF NOT EXISTS "IX_orders_dashboard_order_date"
                ON ordering.orders ((COALESCE("OrderDate", "ZortCreatedAt", "CreatedAtUtc")));
                """);

            migrationBuilder.Sql("""
                CREATE INDEX IF NOT EXISTS "IX_orders_dashboard_shipping_date"
                ON ordering.orders ((COALESCE("ShippingDate", "OrderDate", "ZortUpdatedAt", "ZortCreatedAt", "CreatedAtUtc")));
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DROP INDEX IF EXISTS ordering."IX_orders_dashboard_order_date";
                """);

            migrationBuilder.Sql("""
                DROP INDEX IF EXISTS ordering."IX_orders_dashboard_shipping_date";
                """);
        }
    }
}
