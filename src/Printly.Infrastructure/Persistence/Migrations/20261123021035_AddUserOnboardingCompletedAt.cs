using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Printly.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddUserOnboardingCompletedAt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "OnboardingCompletedAt",
                table: "users",
                type: "datetime(6)",
                nullable: true);

            // Usuarios existentes ja usam o app: nao devem cair no onboarding.
            migrationBuilder.Sql("UPDATE users SET OnboardingCompletedAt = UTC_TIMESTAMP(6) WHERE OnboardingCompletedAt IS NULL;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OnboardingCompletedAt",
                table: "users");
        }
    }
}
