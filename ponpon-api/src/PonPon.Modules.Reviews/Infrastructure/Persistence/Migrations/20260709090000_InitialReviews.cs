using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PonPon.Modules.Reviews.Infrastructure.Persistence.Migrations;

public partial class InitialReviews : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.EnsureSchema(name: "reviews");

        migrationBuilder.CreateTable(
            name: "reviews",
            schema: "reviews",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                VariantId = table.Column<Guid>(type: "uuid", nullable: true),
                OrderId = table.Column<Guid>(type: "uuid", nullable: false),
                OrderItemId = table.Column<Guid>(type: "uuid", nullable: false),
                UserId = table.Column<Guid>(type: "uuid", nullable: false),
                Rating = table.Column<int>(type: "integer", nullable: false),
                Comment = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                EditedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                DeletedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_reviews", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "review_media",
            schema: "reviews",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                ReviewId = table.Column<Guid>(type: "uuid", nullable: false),
                Type = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                Url = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
                ThumbnailUrl = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                DurationSec = table.Column<int>(type: "integer", nullable: true),
                FileSizeBytes = table.Column<long>(type: "bigint", nullable: true),
                MimeType = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                SortOrder = table.Column<int>(type: "integer", nullable: false),
                Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                DeletedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_review_media", x => x.Id);
                table.ForeignKey(
                    name: "FK_review_media_reviews_ReviewId",
                    column: x => x.ReviewId,
                    principalSchema: "reviews",
                    principalTable: "reviews",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_review_media_DeletedAtUtc",
            schema: "reviews",
            table: "review_media",
            column: "DeletedAtUtc");

        migrationBuilder.CreateIndex(
            name: "IX_review_media_ReviewId",
            schema: "reviews",
            table: "review_media",
            column: "ReviewId");

        migrationBuilder.CreateIndex(
            name: "IX_review_media_Status",
            schema: "reviews",
            table: "review_media",
            column: "Status");

        migrationBuilder.CreateIndex(
            name: "IX_reviews_DeletedAtUtc",
            schema: "reviews",
            table: "reviews",
            column: "DeletedAtUtc");

        migrationBuilder.CreateIndex(
            name: "IX_reviews_OrderId",
            schema: "reviews",
            table: "reviews",
            column: "OrderId");

        migrationBuilder.CreateIndex(
            name: "IX_reviews_OrderItemId",
            schema: "reviews",
            table: "reviews",
            column: "OrderItemId",
            unique: true,
            filter: "\"DeletedAtUtc\" IS NULL");

        migrationBuilder.CreateIndex(
            name: "IX_reviews_ProductId",
            schema: "reviews",
            table: "reviews",
            column: "ProductId");

        migrationBuilder.CreateIndex(
            name: "IX_reviews_Status",
            schema: "reviews",
            table: "reviews",
            column: "Status");

        migrationBuilder.CreateIndex(
            name: "IX_reviews_UserId",
            schema: "reviews",
            table: "reviews",
            column: "UserId");

        migrationBuilder.CreateIndex(
            name: "IX_reviews_VariantId",
            schema: "reviews",
            table: "reviews",
            column: "VariantId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "review_media",
            schema: "reviews");

        migrationBuilder.DropTable(
            name: "reviews",
            schema: "reviews");
    }
}
