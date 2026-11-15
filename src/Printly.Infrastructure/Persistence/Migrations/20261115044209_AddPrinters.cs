using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Printly.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPrinters : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AveragePowerWatts",
                table: "user_settings");

            migrationBuilder.DropColumn(
                name: "DefaultProfitMarginPercentage",
                table: "user_settings");

            migrationBuilder.DropColumn(
                name: "DepreciationEnabled",
                table: "user_settings");

            migrationBuilder.DropColumn(
                name: "FailureEnabled",
                table: "user_settings");

            migrationBuilder.DropColumn(
                name: "FailureRatePercentage",
                table: "user_settings");

            migrationBuilder.DropColumn(
                name: "MaintenanceEnabled",
                table: "user_settings");

            migrationBuilder.DropColumn(
                name: "MaintenancePercentage",
                table: "user_settings");

            migrationBuilder.DropColumn(
                name: "PrinterLifespanHours",
                table: "user_settings");

            migrationBuilder.DropColumn(
                name: "PrinterValue",
                table: "user_settings");

            migrationBuilder.AddColumn<Guid>(
                name: "PrinterId",
                table: "products",
                type: "char(36)",
                nullable: true,
                collation: "ascii_general_ci");

            migrationBuilder.CreateTable(
                name: "printer_brands",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    Name = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_printer_brands", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "printer_models",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    Name = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    BrandId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    Image = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_printer_models", x => x.Id);
                    table.ForeignKey(
                        name: "FK_printer_models_printer_brands_BrandId",
                        column: x => x.BrandId,
                        principalTable: "printer_brands",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "printers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    PrinterModelId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    Nickname = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AveragePowerWatts = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    DefaultProfitMarginPercentage = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    PrinterValue = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    PrinterLifespanHours = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    MaintenancePercentage = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    FailureRatePercentage = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    DepreciationEnabled = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    MaintenanceEnabled = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    FailureEnabled = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    UserId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_printers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_printers_printer_models_PrinterModelId",
                        column: x => x.PrinterModelId,
                        principalTable: "printer_models",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_printers_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_products_PrinterId",
                table: "products",
                column: "PrinterId");

            migrationBuilder.CreateIndex(
                name: "IX_printer_models_BrandId",
                table: "printer_models",
                column: "BrandId");

            migrationBuilder.CreateIndex(
                name: "IX_printers_PrinterModelId",
                table: "printers",
                column: "PrinterModelId");

            migrationBuilder.CreateIndex(
                name: "IX_printers_UserId",
                table: "printers",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_products_printers_PrinterId",
                table: "products",
                column: "PrinterId",
                principalTable: "printers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_products_printers_PrinterId",
                table: "products");

            migrationBuilder.DropTable(
                name: "printers");

            migrationBuilder.DropTable(
                name: "printer_models");

            migrationBuilder.DropTable(
                name: "printer_brands");

            migrationBuilder.DropIndex(
                name: "IX_products_PrinterId",
                table: "products");

            migrationBuilder.DropColumn(
                name: "PrinterId",
                table: "products");

            migrationBuilder.AddColumn<decimal>(
                name: "AveragePowerWatts",
                table: "user_settings",
                type: "decimal(10,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "DefaultProfitMarginPercentage",
                table: "user_settings",
                type: "decimal(10,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<bool>(
                name: "DepreciationEnabled",
                table: "user_settings",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "FailureEnabled",
                table: "user_settings",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "FailureRatePercentage",
                table: "user_settings",
                type: "decimal(10,2)",
                nullable: false,
                defaultValue: 5m);

            migrationBuilder.AddColumn<bool>(
                name: "MaintenanceEnabled",
                table: "user_settings",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "MaintenancePercentage",
                table: "user_settings",
                type: "decimal(10,2)",
                nullable: false,
                defaultValue: 10m);

            migrationBuilder.AddColumn<decimal>(
                name: "PrinterLifespanHours",
                table: "user_settings",
                type: "decimal(10,2)",
                nullable: false,
                defaultValue: 2000m);

            migrationBuilder.AddColumn<decimal>(
                name: "PrinterValue",
                table: "user_settings",
                type: "decimal(10,2)",
                nullable: false,
                defaultValue: 0m);
        }
    }
}
