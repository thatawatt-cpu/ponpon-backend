using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using PonPon.Modules.Ordering.Infrastructure.Persistence;

#nullable disable

namespace PonPon.Modules.Ordering.Infrastructure.Persistence.Migrations;

[DbContext(typeof(OrderingDbContext))]
[Migration("20260703140000_AddOrderPricingSnapshot")]
public partial class AddOrderPricingSnapshot : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "PricingSnapshotJson",
            schema: "ordering",
            table: "orders",
            type: "jsonb",
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "PricingSnapshotJson",
            schema: "ordering",
            table: "orders");
    }
}
