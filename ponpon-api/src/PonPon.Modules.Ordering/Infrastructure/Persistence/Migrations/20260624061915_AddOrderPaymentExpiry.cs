using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PonPon.Modules.Ordering.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderPaymentExpiry : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "PaymentExpiresAt",
                schema: "ordering",
                table: "orders",
                type: "timestamp with time zone",
                nullable: true);

            // These columns may already exist if they were applied manually before EF tracking.
            migrationBuilder.Sql("""
                ALTER TABLE ordering.order_items ADD COLUMN IF NOT EXISTS "ImageUrl" character varying(2048);
                ALTER TABLE ordering.order_items ADD COLUMN IF NOT EXISTS "OptionsJson" jsonb;
                ALTER TABLE ordering.order_items ADD COLUMN IF NOT EXISTS "ProductId" uuid;
                ALTER TABLE ordering.order_items ADD COLUMN IF NOT EXISTS "VariantId" uuid;
                """);

            migrationBuilder.CreateIndex(
                name: "IX_orders_PaymentExpiresAt",
                schema: "ordering",
                table: "orders",
                column: "PaymentExpiresAt",
                filter: "\"PaymentExpiresAt\" IS NOT NULL");

            migrationBuilder.Sql("""
                CREATE INDEX IF NOT EXISTS "IX_order_items_ProductId" ON ordering.order_items ("ProductId");
                CREATE INDEX IF NOT EXISTS "IX_order_items_VariantId" ON ordering.order_items ("VariantId");
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_orders_PaymentExpiresAt",
                schema: "ordering",
                table: "orders");

            migrationBuilder.Sql("""
                DROP INDEX IF EXISTS ordering."IX_order_items_ProductId";
                DROP INDEX IF EXISTS ordering."IX_order_items_VariantId";
                """);

            migrationBuilder.DropColumn(
                name: "PaymentExpiresAt",
                schema: "ordering",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "ImageUrl",
                schema: "ordering",
                table: "order_items");

            migrationBuilder.DropColumn(
                name: "OptionsJson",
                schema: "ordering",
                table: "order_items");

            migrationBuilder.DropColumn(
                name: "ProductId",
                schema: "ordering",
                table: "order_items");

            migrationBuilder.DropColumn(
                name: "VariantId",
                schema: "ordering",
                table: "order_items");
        }
    }
}
