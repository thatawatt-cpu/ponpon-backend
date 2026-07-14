using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PonPon.Modules.Identity.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAdminUserManagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PermissionsJson",
                schema: "identity",
                table: "users",
                type: "jsonb",
                nullable: false,
                defaultValueSql: "'[]'::jsonb");

            migrationBuilder.Sql("""
                INSERT INTO identity.roles ("Id", "Name")
                VALUES
                    ('11111111-1111-1111-1111-111111111111', 'Owner'),
                    ('22222222-2222-2222-2222-222222222222', 'Admin'),
                    ('33333333-3333-3333-3333-333333333333', 'Staff')
                ON CONFLICT ("Name") DO NOTHING;

                UPDATE identity.users
                SET "PermissionsJson" = '[
                    "dashboard.read",
                    "orders.read",
                    "orders.manage",
                    "orders.refund",
                    "products.read",
                    "products.manage",
                    "customers.read",
                    "reviews.manage",
                    "marketing.manage",
                    "integrations.read",
                    "admin_users.read"
                ]'::jsonb
                WHERE "Id" IN (
                    SELECT ur."UserId"
                    FROM identity.user_roles ur
                    JOIN identity.roles r ON r."Id" = ur."RoleId"
                    WHERE r."Name" = 'Admin'
                );

                WITH owner_role AS (
                    SELECT "Id" FROM identity.roles WHERE "Name" = 'Owner' LIMIT 1
                ),
                first_admin AS (
                    SELECT u."Id"
                    FROM identity.users u
                    JOIN identity.user_roles ur ON ur."UserId" = u."Id"
                    JOIN identity.roles r ON r."Id" = ur."RoleId"
                    WHERE r."Name" = 'Admin'
                    ORDER BY u."CreatedAtUtc"
                    LIMIT 1
                )
                INSERT INTO identity.user_roles ("UserId", "RoleId")
                SELECT first_admin."Id", owner_role."Id"
                FROM first_admin, owner_role
                ON CONFLICT DO NOTHING;

                UPDATE identity.users
                SET "PermissionsJson" = '["*"]'::jsonb
                WHERE "Id" IN (
                    SELECT u."Id"
                    FROM identity.users u
                    JOIN identity.user_roles ur ON ur."UserId" = u."Id"
                    JOIN identity.roles r ON r."Id" = ur."RoleId"
                    WHERE r."Name" = 'Owner'
                );
                """);

            migrationBuilder.CreateTable(
                name: "admin_user_audit_logs",
                schema: "identity",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ActorUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    TargetUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Action = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    DetailsJson = table.Column<string>(type: "jsonb", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_admin_user_audit_logs", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_admin_user_audit_logs_ActorUserId",
                schema: "identity",
                table: "admin_user_audit_logs",
                column: "ActorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_admin_user_audit_logs_CreatedAtUtc",
                schema: "identity",
                table: "admin_user_audit_logs",
                column: "CreatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_admin_user_audit_logs_TargetUserId",
                schema: "identity",
                table: "admin_user_audit_logs",
                column: "TargetUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "admin_user_audit_logs",
                schema: "identity");

            migrationBuilder.DropColumn(
                name: "PermissionsJson",
                schema: "identity",
                table: "users");
        }
    }
}
