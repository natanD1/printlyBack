using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Printly.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPiecesPerPrintToProduct : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PiecesPerPrint",
                table: "products",
                type: "int",
                nullable: false,
                defaultValue: 1);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PiecesPerPrint",
                table: "products");
        }
    }
}
