using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using PonPon.Modules.Reviews.Infrastructure.Persistence;

#nullable disable

namespace PonPon.Modules.Reviews.Infrastructure.Persistence.Migrations;

[DbContext(typeof(ReviewsDbContext))]
[Migration("20260710100000_AddReviewAnonymousMode")]
public partial class AddReviewAnonymousMode : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<bool>(
            name: "IsAnonymous",
            schema: "reviews",
            table: "reviews",
            type: "boolean",
            nullable: false,
            defaultValue: false);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "IsAnonymous",
            schema: "reviews",
            table: "reviews");
    }
}
