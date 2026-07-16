using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PonPon.Modules.Catalog.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class OptimizeAdminProductList : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS pg_trgm;");

            migrationBuilder.Sql("""
                CREATE INDEX CONCURRENTLY IF NOT EXISTS "IX_products_name_trgm"
                ON catalog.products USING gin ("Name" gin_trgm_ops);
                """, suppressTransaction: true);

            migrationBuilder.Sql("""
                CREATE INDEX CONCURRENTLY IF NOT EXISTS "IX_products_base_sku_trgm"
                ON catalog.products USING gin ("BaseSku" gin_trgm_ops);
                """, suppressTransaction: true);

            migrationBuilder.Sql("""
                CREATE INDEX CONCURRENTLY IF NOT EXISTS "IX_products_barcode_trgm"
                ON catalog.products USING gin ("Barcode" gin_trgm_ops);
                """, suppressTransaction: true);

            migrationBuilder.Sql("""
                CREATE INDEX CONCURRENTLY IF NOT EXISTS "IX_products_admin_sort_date"
                ON catalog.products ((COALESCE("UpdatedAt", "CreatedAt")) DESC);
                """, suppressTransaction: true);

            migrationBuilder.Sql("""
                CREATE INDEX CONCURRENTLY IF NOT EXISTS "IX_products_admin_status_sort_date"
                ON catalog.products ("Status", (COALESCE("UpdatedAt", "CreatedAt")) DESC);
                """, suppressTransaction: true);

            migrationBuilder.Sql("""
                CREATE INDEX CONCURRENTLY IF NOT EXISTS "IX_products_admin_source_sort_date"
                ON catalog.products ("Source", (COALESCE("UpdatedAt", "CreatedAt")) DESC);
                """, suppressTransaction: true);

            migrationBuilder.Sql("""
                CREATE INDEX CONCURRENTLY IF NOT EXISTS "IX_products_admin_status_source_sort_date"
                ON catalog.products ("Status", "Source", (COALESCE("UpdatedAt", "CreatedAt")) DESC);
                """, suppressTransaction: true);

            migrationBuilder.Sql("""
                CREATE INDEX CONCURRENTLY IF NOT EXISTS "IX_products_admin_category_sort_date"
                ON catalog.products ("CategoryName", (COALESCE("UpdatedAt", "CreatedAt")) DESC);
                """, suppressTransaction: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "DROP INDEX CONCURRENTLY IF EXISTS catalog.\"IX_products_name_trgm\";",
                suppressTransaction: true);
            migrationBuilder.Sql(
                "DROP INDEX CONCURRENTLY IF EXISTS catalog.\"IX_products_base_sku_trgm\";",
                suppressTransaction: true);
            migrationBuilder.Sql(
                "DROP INDEX CONCURRENTLY IF EXISTS catalog.\"IX_products_barcode_trgm\";",
                suppressTransaction: true);
            migrationBuilder.Sql(
                "DROP INDEX CONCURRENTLY IF EXISTS catalog.\"IX_products_admin_sort_date\";",
                suppressTransaction: true);
            migrationBuilder.Sql(
                "DROP INDEX CONCURRENTLY IF EXISTS catalog.\"IX_products_admin_status_sort_date\";",
                suppressTransaction: true);
            migrationBuilder.Sql(
                "DROP INDEX CONCURRENTLY IF EXISTS catalog.\"IX_products_admin_source_sort_date\";",
                suppressTransaction: true);
            migrationBuilder.Sql(
                "DROP INDEX CONCURRENTLY IF EXISTS catalog.\"IX_products_admin_status_source_sort_date\";",
                suppressTransaction: true);
            migrationBuilder.Sql(
                "DROP INDEX CONCURRENTLY IF EXISTS catalog.\"IX_products_admin_category_sort_date\";",
                suppressTransaction: true);
        }
    }
}
