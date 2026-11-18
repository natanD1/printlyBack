using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Printly.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddIsProducedToProduct : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsProduced",
                table: "products",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            // Produtos anteriores a essa coluna ja descontaram estoque na criacao: sao produzidos.
            migrationBuilder.Sql("UPDATE products SET IsProduced = 1;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsProduced",
                table: "products");
        }
    }
}
