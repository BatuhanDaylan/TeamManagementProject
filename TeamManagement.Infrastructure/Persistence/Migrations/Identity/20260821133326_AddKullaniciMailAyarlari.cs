using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TeamManagement.Infrastructure.Persistence.Migrations.Identity
{
    /// <inheritdoc />
    public partial class AddKullaniciMailAyarlari : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AiApiKeySifreli",
                table: "Kullanicilar",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MailUygulamaSifresiSifreli",
                table: "Kullanicilar",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "SonIslenenMailUid",
                table: "Kullanicilar",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "SonIslenenMailUidValidity",
                table: "Kullanicilar",
                type: "bigint",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AiApiKeySifreli",
                table: "Kullanicilar");

            migrationBuilder.DropColumn(
                name: "MailUygulamaSifresiSifreli",
                table: "Kullanicilar");

            migrationBuilder.DropColumn(
                name: "SonIslenenMailUid",
                table: "Kullanicilar");

            migrationBuilder.DropColumn(
                name: "SonIslenenMailUidValidity",
                table: "Kullanicilar");
        }
    }
}
