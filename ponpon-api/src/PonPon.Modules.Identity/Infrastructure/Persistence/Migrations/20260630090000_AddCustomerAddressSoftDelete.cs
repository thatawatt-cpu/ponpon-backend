using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PonPon.Modules.Identity.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomerAddressSoftDelete : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_customer_addresses_CustomerId_IsDefault",
                schema: "identity",
                table: "customer_addresses");

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                schema: "identity",
                table: "customer_addresses",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_customer_addresses_CustomerId_IsDefault",
                schema: "identity",
                table: "customer_addresses",
                columns: new[] { "CustomerId", "IsDefault" },
                unique: true,
                filter: "\"IsDefault\" = true AND \"IsDeleted\" = false");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_customer_addresses_CustomerId_IsDefault",
                schema: "identity",
                table: "customer_addresses");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                schema: "identity",
                table: "customer_addresses");

            migrationBuilder.CreateIndex(
                name: "IX_customer_addresses_CustomerId_IsDefault",
                schema: "identity",
                table: "customer_addresses",
                columns: new[] { "CustomerId", "IsDefault" },
                unique: true,
                filter: "\"IsDefault\" = true");
        }
    }
}
