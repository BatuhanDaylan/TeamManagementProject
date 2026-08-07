using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TeamManagement.Infrastructure.Persistence.Migrations.App
{
    /// <inheritdoc />
    public partial class InitialApp : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Bildirimler",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Icerik = table.Column<string>(type: "text", nullable: false),
                    Okundu = table.Column<bool>(type: "boolean", nullable: false),
                    KullaniciId = table.Column<Guid>(type: "uuid", nullable: false),
                    OluşturulmaTarihi = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Bildirimler", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Ekipler",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Ad = table.Column<string>(type: "text", nullable: false),
                    OlusturanLiderId = table.Column<Guid>(type: "uuid", nullable: false),
                    OluşturulmaTarihi = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Ekipler", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Davetler",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Email = table.Column<string>(type: "text", nullable: false),
                    Token = table.Column<string>(type: "text", nullable: false),
                    SonKullanmaTarihi = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Kullanildi = table.Column<bool>(type: "boolean", nullable: false),
                    EkipId = table.Column<Guid>(type: "uuid", nullable: false),
                    OluşturulmaTarihi = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Davetler", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Davetler_Ekipler_EkipId",
                        column: x => x.EkipId,
                        principalTable: "Ekipler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Projeler",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Ad = table.Column<string>(type: "text", nullable: false),
                    EkipId = table.Column<Guid>(type: "uuid", nullable: false),
                    OluşturulmaTarihi = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Projeler", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Projeler_Ekipler_EkipId",
                        column: x => x.EkipId,
                        principalTable: "Ekipler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Gorevler",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Baslik = table.Column<string>(type: "text", nullable: false),
                    Aciklama = table.Column<string>(type: "text", nullable: false),
                    Durum = table.Column<int>(type: "integer", nullable: false),
                    Oncelik = table.Column<string>(type: "text", nullable: false),
                    Deadline = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ProjeId = table.Column<Guid>(type: "uuid", nullable: false),
                    AtananId = table.Column<Guid>(type: "uuid", nullable: true),
                    OluşturulmaTarihi = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Gorevler", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Gorevler_Projeler_ProjeId",
                        column: x => x.ProjeId,
                        principalTable: "Projeler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GorevIlerlemeleri",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TamamlananKisim = table.Column<string>(type: "text", nullable: false),
                    KaynakEmailMetni = table.Column<string>(type: "text", nullable: false),
                    GorevId = table.Column<Guid>(type: "uuid", nullable: false),
                    EkleyenKullaniciId = table.Column<Guid>(type: "uuid", nullable: true),
                    OluşturulmaTarihi = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GorevIlerlemeleri", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GorevIlerlemeleri_Gorevler_GorevId",
                        column: x => x.GorevId,
                        principalTable: "Gorevler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Yorumlar",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Icerik = table.Column<string>(type: "text", nullable: false),
                    GorevId = table.Column<Guid>(type: "uuid", nullable: false),
                    YazanId = table.Column<Guid>(type: "uuid", nullable: false),
                    OluşturulmaTarihi = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Yorumlar", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Yorumlar_Gorevler_GorevId",
                        column: x => x.GorevId,
                        principalTable: "Gorevler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Davetler_EkipId",
                table: "Davetler",
                column: "EkipId");

            migrationBuilder.CreateIndex(
                name: "IX_GorevIlerlemeleri_GorevId",
                table: "GorevIlerlemeleri",
                column: "GorevId");

            migrationBuilder.CreateIndex(
                name: "IX_Gorevler_ProjeId",
                table: "Gorevler",
                column: "ProjeId");

            migrationBuilder.CreateIndex(
                name: "IX_Projeler_EkipId",
                table: "Projeler",
                column: "EkipId");

            migrationBuilder.CreateIndex(
                name: "IX_Yorumlar_GorevId",
                table: "Yorumlar",
                column: "GorevId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Bildirimler");

            migrationBuilder.DropTable(
                name: "Davetler");

            migrationBuilder.DropTable(
                name: "GorevIlerlemeleri");

            migrationBuilder.DropTable(
                name: "Yorumlar");

            migrationBuilder.DropTable(
                name: "Gorevler");

            migrationBuilder.DropTable(
                name: "Projeler");

            migrationBuilder.DropTable(
                name: "Ekipler");
        }
    }
}
