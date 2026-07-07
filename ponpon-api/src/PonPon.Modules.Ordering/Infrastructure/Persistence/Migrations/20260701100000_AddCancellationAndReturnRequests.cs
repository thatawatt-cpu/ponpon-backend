using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using PonPon.Modules.Ordering.Infrastructure.Persistence;

#nullable disable

namespace PonPon.Modules.Ordering.Infrastructure.Persistence.Migrations;

[DbContext(typeof(OrderingDbContext))]
[Migration("20260701100000_AddCancellationAndReturnRequests")]
public partial class AddCancellationAndReturnRequests : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<DateTime>(
            name: "CanceledAtUtc",
            schema: "ordering",
            table: "orders",
            type: "timestamp with time zone",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "CanceledBy",
            schema: "ordering",
            table: "orders",
            type: "character varying(64)",
            maxLength: 64,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "CancellationReason",
            schema: "ordering",
            table: "orders",
            type: "character varying(2000)",
            maxLength: 2000,
            nullable: true);

        migrationBuilder.CreateTable(
            name: "order_return_requests",
            schema: "ordering",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                OrderId = table.Column<Guid>(type: "uuid", nullable: false),
                CustomerId = table.Column<Guid>(type: "uuid", nullable: false),
                Reason = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                Status = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                EvidenceImageUrlsJson = table.Column<string>(type: "jsonb", nullable: false),
                AdminNote = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_order_return_requests", x => x.Id);
                table.ForeignKey(
                    name: "FK_order_return_requests_orders_OrderId",
                    column: x => x.OrderId,
                    principalSchema: "ordering",
                    principalTable: "orders",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_order_return_requests_CustomerId",
            schema: "ordering",
            table: "order_return_requests",
            column: "CustomerId");

        migrationBuilder.CreateIndex(
            name: "IX_order_return_requests_OrderId",
            schema: "ordering",
            table: "order_return_requests",
            column: "OrderId",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_order_return_requests_Status",
            schema: "ordering",
            table: "order_return_requests",
            column: "Status");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "order_return_requests",
            schema: "ordering");

        migrationBuilder.DropColumn(
            name: "CanceledAtUtc",
            schema: "ordering",
            table: "orders");

        migrationBuilder.DropColumn(
            name: "CanceledBy",
            schema: "ordering",
            table: "orders");

        migrationBuilder.DropColumn(
            name: "CancellationReason",
            schema: "ordering",
            table: "orders");
    }
}
