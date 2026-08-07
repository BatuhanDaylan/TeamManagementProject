using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TeamManagement.Infrastructure.Persistence.Migrations.App
{
    /// <inheritdoc />
    public partial class AddHataDurum : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Durum",
                table: "Hatalar",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Durum",
                table: "Hatalar");
        }
    }
}
