using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Printly.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPlans : Migration
    {
        private const string FreePlanId = "5f1c3a7e-2b4d-4e8a-9c6f-0a1b2c3d4e5f";

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "plans",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    Name = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Description = table.Column<string>(type: "varchar(300)", maxLength: 300, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    MonthlyPrice = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    IsDefault = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_plans", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "plan_entitlements",
                columns: table => new
                {
                    PlanId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    Key = table.Column<string>(type: "varchar(60)", maxLength: 60, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IntValue = table.Column<int>(type: "int", nullable: true),
                    BoolValue = table.Column<bool>(type: "tinyint(1)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_plan_entitlements", x => new { x.PlanId, x.Key });
                    table.ForeignKey(
                        name: "FK_plan_entitlements_plans_PlanId",
                        column: x => x.PlanId,
                        principalTable: "plans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_plans_Name",
                table: "plans",
                column: "Name",
                unique: true);

            // Seed via SQL (e nao HasData) pra nao acoplar o snapshot ao plano inicial. Id fixo: o cadastro usa a
            // marcacao IsDefault, mas o backfill abaixo e outros ambientes precisam do mesmo Id.
            migrationBuilder.Sql(
                $"INSERT INTO plans (Id, Name, Description, MonthlyPrice, IsActive, IsDefault, SortOrder, CreatedAt, UpdatedAt) " +
                $"VALUES ('{FreePlanId}', 'Free', NULL, 0, 1, 1, 0, '2026-11-23 00:00:00', NULL);");

            // Free = tudo liberado, com historico de 3 meses anteriores (comportamento atual da home).
            migrationBuilder.Sql(
                "INSERT INTO plan_entitlements (PlanId, `Key`, IntValue, BoolValue) VALUES " +
                $"('{FreePlanId}', 'MaxPrinters', NULL, NULL), " +
                $"('{FreePlanId}', 'MaxFilaments', NULL, NULL), " +
                $"('{FreePlanId}', 'MaxProductsPerMonth', NULL, NULL), " +
                $"('{FreePlanId}', 'ProductHistoryMonths', 3, NULL), " +
                $"('{FreePlanId}', 'CompanyLogoInQuote', NULL, 1), " +
                $"('{FreePlanId}', 'RemoveBrandingFooter', NULL, 1), " +
                $"('{FreePlanId}', 'OptionalCostsEnabled', NULL, 1);");

            // Coluna nasce anulavel, todos os usuarios existentes vao pro Free, e so entao vira obrigatoria com FK.
            migrationBuilder.AddColumn<Guid>(
                name: "PlanId",
                table: "users",
                type: "char(36)",
                nullable: true,
                collation: "ascii_general_ci");

            migrationBuilder.Sql($"UPDATE users SET PlanId = '{FreePlanId}';");

            migrationBuilder.AlterColumn<Guid>(
                name: "PlanId",
                table: "users",
                type: "char(36)",
                nullable: false,
                collation: "ascii_general_ci",
                oldClrType: typeof(Guid),
                oldType: "char(36)",
                oldNullable: true,
                oldCollation: "ascii_general_ci");

            migrationBuilder.CreateIndex(
                name: "IX_users_PlanId",
                table: "users",
                column: "PlanId");

            migrationBuilder.AddForeignKey(
                name: "FK_users_plans_PlanId",
                table: "users",
                column: "PlanId",
                principalTable: "plans",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_users_plans_PlanId",
                table: "users");

            migrationBuilder.DropIndex(
                name: "IX_users_PlanId",
                table: "users");

            migrationBuilder.DropColumn(
                name: "PlanId",
                table: "users");

            migrationBuilder.DropTable(
                name: "plan_entitlements");

            migrationBuilder.DropTable(
                name: "plans");
        }
    }
}
