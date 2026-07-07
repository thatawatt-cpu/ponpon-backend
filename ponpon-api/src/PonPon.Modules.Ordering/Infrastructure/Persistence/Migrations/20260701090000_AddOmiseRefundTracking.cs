using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using PonPon.Modules.Ordering.Infrastructure.Persistence;

#nullable disable

namespace PonPon.Modules.Ordering.Infrastructure.Persistence.Migrations;

[DbContext(typeof(OrderingDbContext))]
[Migration("20260701090000_AddOmiseRefundTracking")]
public partial class AddOmiseRefundTracking : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "OmiseChargeId",
            schema: "ordering",
            table: "orders",
            type: "character varying(128)",
            maxLength: 128,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "OmiseRefundId",
            schema: "ordering",
            table: "orders",
            type: "character varying(128)",
            maxLength: 128,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "OmiseRefundStatus",
            schema: "ordering",
            table: "orders",
            type: "character varying(64)",
            maxLength: 64,
            nullable: true);

        migrationBuilder.AddColumn<DateTime>(
            name: "OmiseRefundedAtUtc",
            schema: "ordering",
            table: "orders",
            type: "timestamp with time zone",
            nullable: true);

        migrationBuilder.AddColumn<decimal>(
            name: "RefundedAmount",
            schema: "ordering",
            table: "orders",
            type: "numeric(18,2)",
            precision: 18,
            scale: 2,
            nullable: false,
            defaultValue: 0m);

        migrationBuilder.CreateIndex(
            name: "IX_orders_OmiseChargeId",
            schema: "ordering",
            table: "orders",
            column: "OmiseChargeId",
            unique: true,
            filter: "\"OmiseChargeId\" IS NOT NULL");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_orders_OmiseChargeId",
            schema: "ordering",
            table: "orders");

        migrationBuilder.DropColumn(
            name: "OmiseChargeId",
            schema: "ordering",
            table: "orders");

        migrationBuilder.DropColumn(
            name: "OmiseRefundId",
            schema: "ordering",
            table: "orders");

        migrationBuilder.DropColumn(
            name: "OmiseRefundStatus",
            schema: "ordering",
            table: "orders");

        migrationBuilder.DropColumn(
            name: "OmiseRefundedAtUtc",
            schema: "ordering",
            table: "orders");

        migrationBuilder.DropColumn(
            name: "RefundedAmount",
            schema: "ordering",
            table: "orders");
    }
}
