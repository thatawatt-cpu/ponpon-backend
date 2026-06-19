using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using PonPon.Modules.Catalog.Infrastructure.Persistence;

#nullable disable

namespace PonPon.Modules.Catalog.Infrastructure.Persistence.Migrations
{
    [DbContext(typeof(CatalogDbContext))]
    [Migration("20260618093000_ReplaceHomeSlideImagesWithImage")]
    public partial class ReplaceHomeSlideImagesWithImage : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Image",
                schema: "catalog",
                table: "home_slides",
                type: "character varying(1024)",
                maxLength: 1024,
                nullable: false,
                defaultValue: "");

            migrationBuilder.Sql("""
                UPDATE catalog.home_slides
                SET "Image" = COALESCE(NULLIF("DesktopImage", ''), NULLIF("MobileImage", ''), '')
                """);

            migrationBuilder.DropColumn(
                name: "DesktopImage",
                schema: "catalog",
                table: "home_slides");

            migrationBuilder.DropColumn(
                name: "MobileImage",
                schema: "catalog",
                table: "home_slides");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DesktopImage",
                schema: "catalog",
                table: "home_slides",
                type: "character varying(1024)",
                maxLength: 1024,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "MobileImage",
                schema: "catalog",
                table: "home_slides",
                type: "character varying(1024)",
                maxLength: 1024,
                nullable: false,
                defaultValue: "");

            migrationBuilder.Sql("""
                UPDATE catalog.home_slides
                SET "DesktopImage" = "Image",
                    "MobileImage" = "Image"
                """);

            migrationBuilder.DropColumn(
                name: "Image",
                schema: "catalog",
                table: "home_slides");
        }
    }
}
