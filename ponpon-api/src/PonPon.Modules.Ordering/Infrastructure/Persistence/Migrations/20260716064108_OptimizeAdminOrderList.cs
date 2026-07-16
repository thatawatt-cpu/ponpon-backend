using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PonPon.Modules.Ordering.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class OptimizeAdminOrderList : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS pg_trgm;");

            migrationBuilder.Sql("""
                CREATE INDEX CONCURRENTLY IF NOT EXISTS "IX_orders_number_trgm"
                ON ordering.orders USING gin ("Number" gin_trgm_ops);
                """, suppressTransaction: true);

            migrationBuilder.Sql("""
                CREATE INDEX CONCURRENTLY IF NOT EXISTS "IX_orders_customer_name_trgm"
                ON ordering.orders USING gin ("CustomerName" gin_trgm_ops);
                """, suppressTransaction: true);

            migrationBuilder.Sql("""
                CREATE INDEX CONCURRENTLY IF NOT EXISTS "IX_orders_customer_phone_trgm"
                ON ordering.orders USING gin ("CustomerPhone" gin_trgm_ops);
                """, suppressTransaction: true);

            migrationBuilder.Sql("""
                CREATE INDEX CONCURRENTLY IF NOT EXISTS "IX_orders_tracking_no_trgm"
                ON ordering.orders USING gin ("TrackingNo" gin_trgm_ops);
                """, suppressTransaction: true);

            migrationBuilder.Sql("""
                CREATE INDEX CONCURRENTLY IF NOT EXISTS "IX_orders_payment_status_sort_date"
                ON ordering.orders ("PaymentStatus", (COALESCE("OrderDate", "ZortCreatedAt", "CreatedAtUtc")) DESC);
                """, suppressTransaction: true);

            migrationBuilder.Sql("""
                CREATE INDEX CONCURRENTLY IF NOT EXISTS "IX_orders_status_sort_date"
                ON ordering.orders ("Status", (COALESCE("OrderDate", "ZortCreatedAt", "CreatedAtUtc")) DESC);
                """, suppressTransaction: true);

            migrationBuilder.Sql("""
                CREATE INDEX CONCURRENTLY IF NOT EXISTS "IX_orders_status_payment_sort_date"
                ON ordering.orders ("Status", "PaymentStatus", (COALESCE("OrderDate", "ZortCreatedAt", "CreatedAtUtc")) DESC);
                """, suppressTransaction: true);

            migrationBuilder.Sql("""
                CREATE INDEX CONCURRENTLY IF NOT EXISTS "IX_orders_shipping_channel_sort_date"
                ON ordering.orders ("ShippingChannel", (COALESCE("OrderDate", "ZortCreatedAt", "CreatedAtUtc")) DESC);
                """, suppressTransaction: true);

            migrationBuilder.Sql("""
                CREATE INDEX CONCURRENTLY IF NOT EXISTS "IX_orders_refund_status_sort_date"
                ON ordering.orders ("OmiseRefundStatus", (COALESCE("OrderDate", "ZortCreatedAt", "CreatedAtUtc")) DESC)
                WHERE "OmiseRefundStatus" IS NOT NULL;
                """, suppressTransaction: true);

            migrationBuilder.Sql("""
                CREATE INDEX CONCURRENTLY IF NOT EXISTS "IX_order_sync_runs_status_completed_at"
                ON ordering.order_sync_runs ("Status", "CompletedAtUtc" DESC)
                WHERE "CompletedAtUtc" IS NOT NULL;
                """, suppressTransaction: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "DROP INDEX CONCURRENTLY IF EXISTS ordering.\"IX_orders_number_trgm\";",
                suppressTransaction: true);
            migrationBuilder.Sql(
                "DROP INDEX CONCURRENTLY IF EXISTS ordering.\"IX_orders_customer_name_trgm\";",
                suppressTransaction: true);
            migrationBuilder.Sql(
                "DROP INDEX CONCURRENTLY IF EXISTS ordering.\"IX_orders_customer_phone_trgm\";",
                suppressTransaction: true);
            migrationBuilder.Sql(
                "DROP INDEX CONCURRENTLY IF EXISTS ordering.\"IX_orders_tracking_no_trgm\";",
                suppressTransaction: true);
            migrationBuilder.Sql(
                "DROP INDEX CONCURRENTLY IF EXISTS ordering.\"IX_orders_payment_status_sort_date\";",
                suppressTransaction: true);
            migrationBuilder.Sql(
                "DROP INDEX CONCURRENTLY IF EXISTS ordering.\"IX_orders_status_sort_date\";",
                suppressTransaction: true);
            migrationBuilder.Sql(
                "DROP INDEX CONCURRENTLY IF EXISTS ordering.\"IX_orders_status_payment_sort_date\";",
                suppressTransaction: true);
            migrationBuilder.Sql(
                "DROP INDEX CONCURRENTLY IF EXISTS ordering.\"IX_orders_shipping_channel_sort_date\";",
                suppressTransaction: true);
            migrationBuilder.Sql(
                "DROP INDEX CONCURRENTLY IF EXISTS ordering.\"IX_orders_refund_status_sort_date\";",
                suppressTransaction: true);
            migrationBuilder.Sql(
                "DROP INDEX CONCURRENTLY IF EXISTS ordering.\"IX_order_sync_runs_status_completed_at\";",
                suppressTransaction: true);

        }
    }
}
