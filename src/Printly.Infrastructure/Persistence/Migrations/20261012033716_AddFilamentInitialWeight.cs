using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Printly.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddFilamentInitialWeight : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "InitialWeight",
                table: "filaments",
                type: "decimal(10,3)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.Sql("UPDATE filaments SET InitialWeight = Weight WHERE InitialWeight = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "InitialWeight",
                table: "filaments");
        }
    }
}
