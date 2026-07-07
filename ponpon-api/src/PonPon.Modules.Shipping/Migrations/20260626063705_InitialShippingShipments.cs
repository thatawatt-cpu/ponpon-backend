using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PonPon.Modules.Shipping.Migrations
{
    /// <inheritdoc />
    public partial class InitialShippingShipments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "shipping");

            migrationBuilder.CreateTable(
                name: "shipping_shipments",
                schema: "shipping",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderId = table.Column<Guid>(type: "uuid", nullable: true),
                    OrderNumber = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    ShippopTrackingCode = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    CourierTrackingCode = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    CourierCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    CourierName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    ShippingStatus = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Price = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    LabelUrl = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    RawBookingJson = table.Column<string>(type: "jsonb", nullable: true),
                    LastWebhookJson = table.Column<string>(type: "jsonb", nullable: true),
                    LastStatusAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_shipping_shipments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "shipping_shipment_events",
                schema: "shipping",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ShipmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    ShippopTrackingCode = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    OrderStatus = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    CourierTrackingCode = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    EventDateTimeUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RawPayloadJson = table.Column<string>(type: "jsonb", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_shipping_shipment_events", x => x.Id);
                    table.ForeignKey(
                        name: "FK_shipping_shipment_events_shipping_shipments_ShipmentId",
                        column: x => x.ShipmentId,
                        principalSchema: "shipping",
                        principalTable: "shipping_shipments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_shipping_shipment_events_EventDateTimeUtc",
                schema: "shipping",
                table: "shipping_shipment_events",
                column: "EventDateTimeUtc");

            migrationBuilder.CreateIndex(
                name: "IX_shipping_shipment_events_OrderStatus",
                schema: "shipping",
                table: "shipping_shipment_events",
                column: "OrderStatus");

            migrationBuilder.CreateIndex(
                name: "IX_shipping_shipment_events_ShipmentId",
                schema: "shipping",
                table: "shipping_shipment_events",
                column: "ShipmentId");

            migrationBuilder.CreateIndex(
                name: "IX_shipping_shipment_events_ShippopTrackingCode",
                schema: "shipping",
                table: "shipping_shipment_events",
                column: "ShippopTrackingCode");

            migrationBuilder.CreateIndex(
                name: "IX_shipping_shipments_CourierTrackingCode",
                schema: "shipping",
                table: "shipping_shipments",
                column: "CourierTrackingCode");

            migrationBuilder.CreateIndex(
                name: "IX_shipping_shipments_OrderId",
                schema: "shipping",
                table: "shipping_shipments",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_shipping_shipments_OrderNumber",
                schema: "shipping",
                table: "shipping_shipments",
                column: "OrderNumber");

            migrationBuilder.CreateIndex(
                name: "IX_shipping_shipments_ShippingStatus",
                schema: "shipping",
                table: "shipping_shipments",
                column: "ShippingStatus");

            migrationBuilder.CreateIndex(
                name: "IX_shipping_shipments_ShippopTrackingCode",
                schema: "shipping",
                table: "shipping_shipments",
                column: "ShippopTrackingCode",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "shipping_shipment_events",
                schema: "shipping");

            migrationBuilder.DropTable(
                name: "shipping_shipments",
                schema: "shipping");
        }
    }
}
