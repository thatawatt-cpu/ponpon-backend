using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using PonPon.Modules.Catalog.Infrastructure.Persistence;

#nullable disable

namespace PonPon.Modules.Catalog.Infrastructure.Persistence.Migrations
{
    [DbContext(typeof(CatalogDbContext))]
    [Migration("20260618101500_AddHomeSlideBadge")]
    public partial class AddHomeSlideBadge : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Badge",
                schema: "catalog",
                table: "home_slides",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Badge",
                schema: "catalog",
                table: "home_slides");
        }
    }
}
