using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PonPon.Modules.Ordering.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderCustomerOwnership : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CustomerId",
                schema: "ordering",
                table: "orders",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_orders_CustomerId",
                schema: "ordering",
                table: "orders",
                column: "CustomerId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_orders_CustomerId",
                schema: "ordering",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "CustomerId",
                schema: "ordering",
                table: "orders");
        }
    }
}
