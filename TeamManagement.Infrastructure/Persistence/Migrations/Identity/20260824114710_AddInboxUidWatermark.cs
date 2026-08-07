using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TeamManagement.Infrastructure.Persistence.Migrations.Identity
{
    /// <inheritdoc />
    public partial class AddInboxUidWatermark : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "SonIslenenInboxUid",
                table: "Kullanicilar",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "SonIslenenInboxUidValidity",
                table: "Kullanicilar",
                type: "bigint",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SonIslenenInboxUid",
                table: "Kullanicilar");

            migrationBuilder.DropColumn(
                name: "SonIslenenInboxUidValidity",
                table: "Kullanicilar");
        }
    }
}
