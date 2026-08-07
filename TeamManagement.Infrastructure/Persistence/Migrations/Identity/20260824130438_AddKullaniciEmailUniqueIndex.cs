using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TeamManagement.Infrastructure.Persistence.Migrations.Identity
{
    /// <inheritdoc />
    public partial class AddKullaniciEmailUniqueIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Kullanicilar_Email",
                table: "Kullanicilar",
                column: "Email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Kullanicilar_Email",
                table: "Kullanicilar");
        }
    }
}
