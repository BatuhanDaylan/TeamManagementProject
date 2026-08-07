using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TeamManagement.Infrastructure.Persistence.Migrations.App
{
    /// <inheritdoc />
    public partial class AddGorevBaslangicTarihiAndIptalDurumu : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Hatalar_Gorevler_GorevId",
                table: "Hatalar");

            migrationBuilder.DropIndex(
                name: "IX_Hatalar_GorevId",
                table: "Hatalar");

            migrationBuilder.AddColumn<Guid>(
                name: "Gorev",
                table: "Hatalar",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<DateTime>(
                name: "BaslangicTarihi",
                table: "Gorevler",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Gorev",
                table: "Hatalar");

            migrationBuilder.DropColumn(
                name: "BaslangicTarihi",
                table: "Gorevler");

            migrationBuilder.CreateIndex(
                name: "IX_Hatalar_GorevId",
                table: "Hatalar",
                column: "GorevId");

            migrationBuilder.AddForeignKey(
                name: "FK_Hatalar_Gorevler_GorevId",
                table: "Hatalar",
                column: "GorevId",
                principalTable: "Gorevler",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
