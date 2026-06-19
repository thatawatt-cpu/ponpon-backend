using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PonPon.Modules.Ordering.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderIntegrationCustomer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "IntegrationCustomer",
                schema: "ordering",
                table: "orders",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IntegrationCustomerId",
                schema: "ordering",
                table: "orders",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_orders_IntegrationCustomerId",
                schema: "ordering",
                table: "orders",
                column: "IntegrationCustomerId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_orders_IntegrationCustomerId",
                schema: "ordering",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "IntegrationCustomer",
                schema: "ordering",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "IntegrationCustomerId",
                schema: "ordering",
                table: "orders");
        }
    }
}
