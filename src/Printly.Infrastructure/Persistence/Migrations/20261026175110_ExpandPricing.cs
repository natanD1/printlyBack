using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Printly.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ExpandPricing : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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

            migrationBuilder.AddColumn<int>(
                name: "Quantity",
                table: "products",
                type: "int",
                nullable: false,
                defaultValue: 1);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
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

            migrationBuilder.DropColumn(
                name: "Quantity",
                table: "products");
        }
    }
}
