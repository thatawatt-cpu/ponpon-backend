using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PonPon.Modules.Ordering.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialOrdering : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "ordering");

            migrationBuilder.CreateTable(
                name: "orders",
                schema: "ordering",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ZortOrderId = table.Column<long>(type: "bigint", nullable: false),
                    Number = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    ZortCustomerId = table.Column<long>(type: "bigint", nullable: true),
                    CustomerCode = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    CustomerName = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    CustomerIdNumber = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    CustomerEmail = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: true),
                    CustomerPhone = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    CustomerAddress = table.Column<string>(type: "character varying(4096)", maxLength: 4096, nullable: true),
                    Status = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    PaymentStatus = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    VatAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    ShippingAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    PaymentAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    DiscountAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    ShippingChannel = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    ShippingName = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    ShippingAddress = table.Column<string>(type: "character varying(4096)", maxLength: 4096, nullable: true),
                    ShippingPhone = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    TrackingNo = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    OrderDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ShippingDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Reference = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    Description = table.Column<string>(type: "character varying(4096)", maxLength: 4096, nullable: true),
                    SalesChannel = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    WarehouseCode = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    IsCod = table.Column<bool>(type: "boolean", nullable: false),
                    Currency = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true),
                    TagsJson = table.Column<string>(type: "jsonb", nullable: true),
                    ZortCreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ZortUpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RawZortJson = table.Column<string>(type: "jsonb", nullable: false),
                    LastSyncedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_orders", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "order_items",
                schema: "ordering",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    ZortProductId = table.Column<long>(type: "bigint", nullable: true),
                    Sku = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Name = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    UnitText = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    PricePerUnit = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Discount = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    DiscountAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalPrice = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    ProductType = table.Column<int>(type: "integer", nullable: false),
                    BundleId = table.Column<long>(type: "bigint", nullable: true),
                    BundleCode = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    BundleName = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    RawZortJson = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_order_items", x => x.Id);
                    table.ForeignKey(
                        name: "FK_order_items_orders_OrderId",
                        column: x => x.OrderId,
                        principalSchema: "ordering",
                        principalTable: "orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "order_payments",
                schema: "ordering",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    ZortPaymentId = table.Column<long>(type: "bigint", nullable: true),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    PaymentDateTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RawZortJson = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_order_payments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_order_payments_orders_OrderId",
                        column: x => x.OrderId,
                        principalSchema: "ordering",
                        principalTable: "orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_order_items_OrderId",
                schema: "ordering",
                table: "order_items",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_order_items_ZortProductId",
                schema: "ordering",
                table: "order_items",
                column: "ZortProductId");

            migrationBuilder.CreateIndex(
                name: "IX_order_payments_OrderId",
                schema: "ordering",
                table: "order_payments",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_order_payments_ZortPaymentId",
                schema: "ordering",
                table: "order_payments",
                column: "ZortPaymentId");

            migrationBuilder.CreateIndex(
                name: "IX_orders_Number",
                schema: "ordering",
                table: "orders",
                column: "Number");

            migrationBuilder.CreateIndex(
                name: "IX_orders_SalesChannel_OrderDate",
                schema: "ordering",
                table: "orders",
                columns: new[] { "SalesChannel", "OrderDate" });

            migrationBuilder.CreateIndex(
                name: "IX_orders_Status_PaymentStatus",
                schema: "ordering",
                table: "orders",
                columns: new[] { "Status", "PaymentStatus" });

            migrationBuilder.CreateIndex(
                name: "IX_orders_ZortOrderId",
                schema: "ordering",
                table: "orders",
                column: "ZortOrderId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "order_items",
                schema: "ordering");

            migrationBuilder.DropTable(
                name: "order_payments",
                schema: "ordering");

            migrationBuilder.DropTable(
                name: "orders",
                schema: "ordering");
        }
    }
}
