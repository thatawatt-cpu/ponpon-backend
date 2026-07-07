using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using PonPon.Modules.Ordering.Infrastructure.Persistence;

#nullable disable

namespace PonPon.Modules.Ordering.Infrastructure.Persistence.Migrations;

[DbContext(typeof(OrderingDbContext))]
[Migration("20260703130000_AddPaymentCreationGuard")]
public partial class AddPaymentCreationGuard : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<bool>(
            name: "IsPaymentCreationPending",
            schema: "ordering",
            table: "orders",
            type: "boolean",
            nullable: false,
            defaultValue: false);

        migrationBuilder.AddColumn<DateTime>(
            name: "PaymentCreationStartedAtUtc",
            schema: "ordering",
            table: "orders",
            type: "timestamp with time zone",
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "IsPaymentCreationPending",
            schema: "ordering",
            table: "orders");

        migrationBuilder.DropColumn(
            name: "PaymentCreationStartedAtUtc",
            schema: "ordering",
            table: "orders");
    }
}
