using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TeamManagement.Infrastructure.Persistence.Migrations.App
{
    /// <inheritdoc />
    public partial class AddEkipOzetGecmisi : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EkipOzetGecmisleri",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Icerik = table.Column<string>(type: "text", nullable: false),
                    EkipId = table.Column<Guid>(type: "uuid", nullable: false),
                    OluşturulmaTarihi = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EkipOzetGecmisleri", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EkipOzetGecmisleri_Ekipler_EkipId",
                        column: x => x.EkipId,
                        principalTable: "Ekipler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EkipOzetGecmisleri_EkipId",
                table: "EkipOzetGecmisleri",
                column: "EkipId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EkipOzetGecmisleri");
        }
    }
}
