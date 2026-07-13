using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PonPon.Modules.Catalog.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddFlashSalePublishStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PublishStatus",
                schema: "catalog",
                table: "flash_sales",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "draft");

            migrationBuilder.Sql("""
                UPDATE catalog.flash_sales
                SET "PublishStatus" = 'published'
                """);

            migrationBuilder.CreateIndex(
                name: "IX_flash_sales_PublishStatus_StartDate_EndDate",
                schema: "catalog",
                table: "flash_sales",
                columns: new[] { "PublishStatus", "StartDate", "EndDate" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_flash_sales_PublishStatus_StartDate_EndDate",
                schema: "catalog",
                table: "flash_sales");

            migrationBuilder.DropColumn(
                name: "PublishStatus",
                schema: "catalog",
                table: "flash_sales");
        }
    }
}
