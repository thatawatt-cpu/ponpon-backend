using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using PonPon.Modules.Ordering.Infrastructure.Persistence;

#nullable disable

namespace PonPon.Modules.Ordering.Infrastructure.Persistence.Migrations;

[DbContext(typeof(OrderingDbContext))]
[Migration("20260703120000_AddOrderStockReservation")]
public partial class AddOrderStockReservation : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<bool>(
            name: "HasStockReservation",
            schema: "ordering",
            table: "orders",
            type: "boolean",
            nullable: false,
            defaultValue: false);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "HasStockReservation",
            schema: "ordering",
            table: "orders");
    }
}
