using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PonPon.Modules.Ordering.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(OrderingDbContext))]
    [Migration("20260708120000_AddCheckoutQuotes")]
    public partial class AddCheckoutQuotes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "checkout_quotes",
                schema: "ordering",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uuid", nullable: false),
                    PayloadHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    CalculationHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    PricingSnapshotJson = table.Column<string>(type: "jsonb", nullable: false),
                    ExpiresAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsFinal = table.Column<bool>(type: "boolean", nullable: false),
                    CalculationStatus = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    ShippingFinalized = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UsedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ClientRequestId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_checkout_quotes", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_checkout_quotes_ClientRequestId",
                schema: "ordering",
                table: "checkout_quotes",
                column: "ClientRequestId",
                unique: true,
                filter: "\"ClientRequestId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_checkout_quotes_CustomerId",
                schema: "ordering",
                table: "checkout_quotes",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_checkout_quotes_ExpiresAtUtc",
                schema: "ordering",
                table: "checkout_quotes",
                column: "ExpiresAtUtc");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "checkout_quotes",
                schema: "ordering");
        }
    }
}
