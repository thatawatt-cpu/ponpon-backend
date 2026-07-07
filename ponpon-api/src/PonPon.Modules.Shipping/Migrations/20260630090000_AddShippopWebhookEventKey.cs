using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using PonPon.Modules.Shipping.Infrastructure.Persistence;

#nullable disable

namespace PonPon.Modules.Shipping.Migrations;

[DbContext(typeof(ShippingDbContext))]
[Migration("20260630090000_AddShippopWebhookEventKey")]
public partial class AddShippopWebhookEventKey : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "WebhookEventKey",
            schema: "shipping",
            table: "shipping_shipment_events",
            type: "character varying(64)",
            maxLength: 64,
            nullable: true);

        migrationBuilder.CreateIndex(
            name: "IX_shipping_shipment_events_WebhookEventKey",
            schema: "shipping",
            table: "shipping_shipment_events",
            column: "WebhookEventKey",
            unique: true,
            filter: "\"WebhookEventKey\" IS NOT NULL");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_shipping_shipment_events_WebhookEventKey",
            schema: "shipping",
            table: "shipping_shipment_events");

        migrationBuilder.DropColumn(
            name: "WebhookEventKey",
            schema: "shipping",
            table: "shipping_shipment_events");
    }
}
