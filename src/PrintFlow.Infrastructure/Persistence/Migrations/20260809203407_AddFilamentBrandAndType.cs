using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PrintFlow.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddFilamentBrandAndType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Brand",
                table: "filaments");

            migrationBuilder.AddColumn<Guid>(
                name: "BrandId",
                table: "filaments",
                type: "char(36)",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                collation: "ascii_general_ci");

            migrationBuilder.AddColumn<Guid>(
                name: "TypeId",
                table: "filaments",
                type: "char(36)",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                collation: "ascii_general_ci");

            migrationBuilder.CreateTable(
                name: "filaments_brands",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    NameBrand = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ImageBrand = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_filaments_brands", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "filaments_type",
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
                    table.PrimaryKey("PK_filaments_type", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_filaments_BrandId",
                table: "filaments",
                column: "BrandId");

            migrationBuilder.CreateIndex(
                name: "IX_filaments_TypeId",
                table: "filaments",
                column: "TypeId");

            migrationBuilder.AddForeignKey(
                name: "FK_filaments_filaments_brands_BrandId",
                table: "filaments",
                column: "BrandId",
                principalTable: "filaments_brands",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_filaments_filaments_type_TypeId",
                table: "filaments",
                column: "TypeId",
                principalTable: "filaments_type",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_filaments_filaments_brands_BrandId",
                table: "filaments");

            migrationBuilder.DropForeignKey(
                name: "FK_filaments_filaments_type_TypeId",
                table: "filaments");

            migrationBuilder.DropTable(
                name: "filaments_brands");

            migrationBuilder.DropTable(
                name: "filaments_type");

            migrationBuilder.DropIndex(
                name: "IX_filaments_BrandId",
                table: "filaments");

            migrationBuilder.DropIndex(
                name: "IX_filaments_TypeId",
                table: "filaments");

            migrationBuilder.DropColumn(
                name: "BrandId",
                table: "filaments");

            migrationBuilder.DropColumn(
                name: "TypeId",
                table: "filaments");

            migrationBuilder.AddColumn<string>(
                name: "Brand",
                table: "filaments",
                type: "varchar(150)",
                maxLength: 150,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");
        }
    }
}
